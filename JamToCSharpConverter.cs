using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using jamconverter.AST;
using jamconverter.Tests;
using NiceIO;
using runtimelib;
using NRefactory = ICSharpCode.NRefactory.CSharp;

namespace jamconverter
{
    class JamToCSharpConverter
    {
        private NRefactory.TypeDeclaration typeForJamFile;
        private NRefactory.TypeDeclaration _staticGlobals;
		private IEnumerable<ActionsDeclarationStatement> _allActions;
		private IEnumerable<RuleDeclarationStatement> _allRules;
	    private readonly Dictionary<SourceFileDescription, TopLevel> _filesToTopLevel = new Dictionary<SourceFileDescription, TopLevel>();
        private readonly string _variableRestorerVariableName = "variableRestorer";

        public string Convert(string simpleProgram)
	    {
	        var sd = new SourceFileDescription() {Contents = simpleProgram, File = new NPath("Jamfile.jam")};

	        return Convert(new ProgramDescripton {sd})[0].Contents;
	    }

        public static IEnumerable<NPath> RuntimeDependencies
        {
            get { yield return JamRunner.ConverterRoot.Combine(new NPath("bin/runtimelib.dll")); }
        }

		public ProgramDescripton Convert(ProgramDescripton jamProgram)
        {
			foreach (var sourceFile in jamProgram)
			{
			    try
			    {
			        _filesToTopLevel.Add(sourceFile, new Parser(sourceFile.Contents).ParseTopLevel());
			    }
			    catch (Exception e)
			    {
			        Console.WriteLine("Parsing error in: "+sourceFile.File);
			    }
			}
		    _allActions = _filesToTopLevel.Values.SelectMany(top => top.GetAllChildrenOfType<ActionsDeclarationStatement>()).ToArray();
		    _allRules = _filesToTopLevel.Values.SelectMany(top => top.GetAllChildrenOfType<RuleDeclarationStatement>()).ToArray();

			var globalsFile = NewSyntaxTree();
			_staticGlobals = new NRefactory.TypeDeclaration { Name = "StaticGlobals", BaseTypes = { new NRefactory.SimpleType("GlobalVariables") } };
			var staticGlobalsSingleton = new NRefactory.TypeDeclaration { Name = "StaticGlobalsSingleton" };
			var staticGlobalsType = new NRefactory.SimpleType(_staticGlobals.Name);
			staticGlobalsSingleton.Members.Add(new NRefactory.FieldDeclaration()
			{
				ReturnType = staticGlobalsType.Clone(),
				Modifiers = NRefactory.Modifiers.Static,
				Variables = { new NRefactory.VariableInitializer("Globals", new NRefactory.ObjectCreateExpression(staticGlobalsType.Clone()))}
			});
			

			globalsFile.Members.Add(staticGlobalsSingleton);
			globalsFile.Members.Add(_staticGlobals);

			
			var result = new ProgramDescripton();
			
			foreach (var kvp in _filesToTopLevel)
			{
				var file = kvp.Key;
				var topLevel = kvp.Value;

				var syntaxTree = NewSyntaxTree();
				typeForJamFile = new NRefactory.TypeDeclaration { Name = ConverterLogic.ClassNameForJamFile(file.File) };
				syntaxTree.Members.Add(typeForJamFile);


			    try
			    {
			        typeForJamFile.Members.Add(new NRefactory.FieldDeclaration()
			        {
			            ReturnType = StaticGlobalsType,
			            Modifiers = NRefactory.Modifiers.Static,
			            Variables = {new NRefactory.VariableInitializer("Globals", new NRefactory.ObjectCreateExpression(StaticGlobalsType))}
			        });

			        var body = new NRefactory.BlockStatement();
			        foreach (var statement in topLevel.Statements)
			        {
			            body.Statements.AddRange(ProcessStatement(statement));
			        }

			        var allLocalStatements = topLevel.Statements.SelectMany(s => s.GetAllChildrenOfType<LocalStatement>()).Concat(topLevel.Statements.OfType<LocalStatement>());
			        var topLevelLocalsOnly = allLocalStatements.Where(l => FindParentOfType<RuleDeclarationStatement>(l) == null);
                    if (topLevelLocalsOnly.Any())
			        {
			            var variableRestorerType = new NRefactory.SimpleType(nameof(VariableRestorer));
			            var variableRestoreUsing = new NRefactory.UsingStatement()
			            {
			                EmbeddedStatement = body,
			                ResourceAcquisition =
			                    new NRefactory.VariableDeclarationStatement(variableRestorerType.Clone(), _variableRestorerVariableName, new NRefactory.ObjectCreateExpression(variableRestorerType.Clone())),
			            };

			            var topLevelBody = new NRefactory.BlockStatement();
			            topLevelBody.Statements.Add(variableRestoreUsing);
			            body = topLevelBody;
			        }

			        typeForJamFile.Members.Add(new NRefactory.MethodDeclaration
			        {
			            Name = "TopLevel",
			            ReturnType = new NRefactory.PrimitiveType("void"),
			            Modifiers = NRefactory.Modifiers.Static | NRefactory.Modifiers.Public,
			            Body = body
                    });
			        result.Add(new SourceFileDescription() {File = new NPath(typeForJamFile.Name + ".cs"), Contents = syntaxTree.ToString()});
			    }
			    catch (Exception e)
			    {
			        Console.WriteLine($"failed converting {file.File.FileName}");
			    }
			}

		
			result.Add(BuildActionsType());
			result.Add(BuildEntryPoint());

			var globalsFileDescription = new SourceFileDescription()
			{
				File = new NPath("StaticGlobals.cs"),
				Contents = globalsFile.ToString()
			};
			result.Add(globalsFileDescription);
			return result;
        }

	    private SourceFileDescription BuildEntryPoint()
	    {		
		    var syntaxTree = NewSyntaxTree();
		    var entrypointType = new NRefactory.TypeDeclaration() {Name = "EntryPoint"};
		    var mainMethod = new NRefactory.MethodDeclaration
		    {
			    Name = "Main",
			    Modifiers = NRefactory.Modifiers.Static | NRefactory.Modifiers.Public,
			    Body = new NRefactory.BlockStatement(),
				ReturnType = new NRefactory.PrimitiveType("void")
		    };

			var types =
				_filesToTopLevel.Keys.Select (file => ConverterLogic.ClassNameForJamFile (file.File))
				    .Select (name => new NRefactory.SimpleType (name))
					.Prepend (new NRefactory.SimpleType ("Actions"))
					.Prepend (new NRefactory.SimpleType ("BuiltinFunctions"))
					.Select (st => new NRefactory.TypeOfExpression (st));


	        foreach (var file in _filesToTopLevel.Keys)
	        {
	            var filesRegistration =
	                new NRefactory.ExpressionStatement(
	                    new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression("BuiltinFunctions"), "RegisterJamFile"),
	                        new NRefactory.PrimitiveExpression(file.File.ToString(SlashMode.Forward)), new NRefactory.IdentifierExpression(ConverterLogic.ClassNameForJamFile(file.File)+".TopLevel")));

	            mainMethod.Body.Statements.Add(filesRegistration);
	        }

	        var objectCreateExpression = new NRefactory.ObjectCreateExpression(new NRefactory.SimpleType(nameof(DynamicRuleInvocationService)), types);
		    var assignment =
			    new NRefactory.AssignmentExpression(new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression(nameof(DynamicRuleInvocationService)),"Instance"), objectCreateExpression);

			mainMethod.Body.Statements.Add(assignment);

	        syntaxTree.Members.Add(entrypointType);
			entrypointType.Members.Add(mainMethod);

			return new SourceFileDescription()
		    {
			    File = new NPath("EntryPoint.cs"),
			    Contents = syntaxTree.ToString()
			};
	    }

	    private SourceFileDescription BuildActionsType()
	    {
		    var actions = new NRefactory.TypeDeclaration
		    {
			    ClassType = NRefactory.ClassType.Class,
			    Name = "Actions",
		    };
		    foreach (var action in _allActions.DistinctBy(a => a.Name))
		    {
			    var actionWrapperBody = new NRefactory.BlockStatement();

			    actionWrapperBody.Statements.Add(new NRefactory.ReturnStatement(
				    new NRefactory.InvocationExpression(new NRefactory.IdentifierExpression("InvokeRule"), new NRefactory.Expression[]
				    {
					    new NRefactory.PrimitiveExpression(ConverterLogic.CleanIllegalCharacters(action.Name)),
					    new NRefactory.IdentifierExpression("values"),
				    })
				    ));
			    var actionMethod = new NRefactory.MethodDeclaration
			    {
				    Name = ConverterLogic.CleanIllegalCharacters(action.Name),
				    ReturnType = LocalJamListAstType,
				    Modifiers = NRefactory.Modifiers.Static | NRefactory.Modifiers.Public,
				    Body = actionWrapperBody
			    };
			    actionMethod.Parameters.Add(new NRefactory.ParameterDeclaration(new NRefactory.PrimitiveType("JamListBase[]"), "values",
				    NRefactory.ParameterModifier.Params));
			    actions.Members.Add(actionMethod);
		    }
		    var syntaxtree = NewSyntaxTree();
		    syntaxtree.Members.Add(actions);
		    var sourceFileDescription = new SourceFileDescription() {File = new NPath("Actions.cs"), Contents = syntaxtree.ToString()};
		    return sourceFileDescription;
	    }

	    private static NRefactory.SyntaxTree NewSyntaxTree()
	    {
		    var syntaxTree = new NRefactory.SyntaxTree();
			syntaxTree.Members.Add(new NRefactory.UsingDeclaration("System"));
			syntaxTree.Members.Add(new NRefactory.UsingDeclaration("System.Reflection"));
		    syntaxTree.Members.Add(new NRefactory.UsingDeclaration("System.Linq"));
		    syntaxTree.Members.Add(new NRefactory.UsingDeclaration("runtimelib"));
		    syntaxTree.Members.Add(new NRefactory.UsingDeclaration("static BuiltinFunctions"));
			syntaxTree.Members.Add(new NRefactory.UsingDeclaration("static StaticGlobalsSingleton"));
			return syntaxTree;
	    }

	    private static NRefactory.SimpleType DynamicRuleInvocationServiceType
	    {
		    get { return new NRefactory.SimpleType(nameof(DynamicRuleInvocationService)); }
	    }

	    private NRefactory.SimpleType StaticGlobalsType => new NRefactory.SimpleType(_staticGlobals.Name);

        private IEnumerable<NRefactory.Statement> ProcessStatement(Statement statement)
        {
            if (statement == null)
                return new NRefactory.Statement[0];

            if (statement is IfStatement)
            {
                return new[] {ProcessIfStatement((IfStatement) statement)};
            }
            if (statement is WhileStatement)
                return new[] { ProcessWhileStatement((WhileStatement) statement) };

            if (statement is RuleDeclarationStatement)
                return new[] {ProcessRuleDeclarationStatement((RuleDeclarationStatement) statement)};

            if (statement is ReturnStatement)
                return new[] { ProcessReturnStatement(statement)};

            if (statement is ForStatement)
                return new[] { ProcessForStatement((ForStatement) statement)};

            if (statement is BreakStatement)
                return new[] { new NRefactory.BreakStatement()};

            if (statement is ContinueStatement)
                return new[] { new NRefactory.ContinueStatement()};

            if (statement is BlockStatement)
                return new[] { ProcessBlockStatement((BlockStatement) statement)};

            if (statement is SwitchStatement)
                return new[] { ProcessSwitchStatement((SwitchStatement) statement)};

	        if (statement is LocalStatement)
                return ProcessLocalStatement((LocalStatement) statement);

	        if (statement is ActionsDeclarationStatement)
                return new[] { ProcessActionsDeclarationStatement((ActionsDeclarationStatement) statement)};
			
			if (statement is AssignmentStatement)
			{
				AssignmentStatement assignmentStatement = (AssignmentStatement)statement;
                return new[] { ProcessAssignment(assignmentStatement.Left, assignmentStatement.Operator, assignmentStatement.Right)};
			}

	        if (statement is OnStatement)
                return new[] { ProcessOnStatement((OnStatement) statement)};

	        if (statement is IncludeStatement)
                return new[] { ProcessIncludeStatement((IncludeStatement) statement)};

            return new[] { ProcessExpressionStatement((ExpressionStatement) statement)};
        }

        private NRefactory.Statement ProcessReturnStatement(Statement statement)
        {
            NRefactory.Expression returnExpression = null;
            if (FindParentOfType<RuleDeclarationStatement>(statement) != null)
                returnExpression = ProcessExpressionList(statement.As<ReturnStatement>().ReturnExpression, mightModify: true);
            return new NRefactory.ReturnStatement(returnExpression);
        }

        private NRefactory.Statement ProcessIncludeStatement(IncludeStatement statement)
	    {
            /*
		    var literal = statement.Expression as LiteralExpression;
		    if (literal != null)
		    {
			    var memberReferenceExpression = new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression(ConverterLogic.ClassNameForJamFile(literal.Value)), "TopLevel");
			    return new NRefactory.ExpressionStatement(new NRefactory.InvocationExpression(memberReferenceExpression));
		    }*/

			var memberReferenceExpression2 = new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression(nameof(DynamicRuleInvocationService)),"Instance.DynamicInclude");
			return new NRefactory.ExpressionStatement(new NRefactory.InvocationExpression(memberReferenceExpression2, ProcessExpression(statement.Expression)));
		}

		private NRefactory.Expression GetActionModifiers (ActionsDeclarationStatement statement)
		{
			if (!statement.Modifiers.Any())
				return new NRefactory.IdentifierExpression("Jam.ActionsFlags.None");

			NRefactory.Expression result = null;

			foreach (LiteralExpression modifier in statement.Modifiers)
			{
				foreach (var name in Enum.GetNames(typeof(Jam.ActionsFlags))) {
					if (modifier.Value.ToLower() == name.ToLower()){
						var modifierExpression = new NRefactory.IdentifierExpression("Jam.ActionsFlags."+name);
						if (result == null)
							result = modifierExpression;
						else 
							result = new NRefactory.BinaryOperatorExpression(result, NRefactory.BinaryOperatorType.BitwiseOr, modifierExpression);
						break;
					}
				}
			}

			return result;
		}

		private int GetModifierValue (ActionsDeclarationStatement statement, Jam.ActionsFlags flag)
		{
			for (int i = 0; i < statement.Modifiers.Count (); i++) 
			{
				var modifier = statement.Modifiers [i] as LiteralExpression;
				if (modifier.Value.ToLower () == flag.ToString ().ToLower ())
				{
					modifier = statement.Modifiers [i + 1] as LiteralExpression;
					return System.Int32.Parse (modifier.Value);
				}
			}
			return 0;
		}

	    private NRefactory.Statement ProcessOnStatement(OnStatement statement)
	    {
		    var onStartContextMethod = new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression("Globals"), "OnTargetContext");

		    var onStartContextInvocation = new NRefactory.InvocationExpression(onStartContextMethod, ProcessExpression(statement.Target));
			
		    return new NRefactory.UsingStatement
		    {
			    ResourceAcquisition = onStartContextInvocation,
			    EmbeddedStatement = ProcessStatement(statement.Body).Single()
		    };
		}

	    private NRefactory.Statement ProcessActionsDeclarationStatement(ActionsDeclarationStatement statement)
	    {
			return new NRefactory.InvocationExpression(new NRefactory.IdentifierExpression("MakeActions"), new NRefactory.Expression[]{
				new NRefactory.PrimitiveExpression(statement.Name),
				new NRefactory.PrimitiveExpression(statement.Actions),
				GetActionModifiers(statement),
				new NRefactory.PrimitiveExpression(GetModifierValue(statement, Jam.ActionsFlags.MaxTargets)),
				new NRefactory.PrimitiveExpression(GetModifierValue(statement, Jam.ActionsFlags.MaxLine))
			});
	    }
    
	    private IEnumerable<NRefactory.Statement> ProcessLocalStatement(LocalStatement statement)
	    {
	        if (IsThisLocalOnlyUsedToNotMakeAnUpcomingForLoopNotWriteToGlobal(statement))
	            yield break;
            
            //because it happens so often that we have jamfiles that write to a local in a file, and then expect it to be usable in an upcoming rule,  let's convert these locals to globals instead. how does anything even work?!
	        if (IsInTopLevel(statement))
	        {
	            var restoreafterfunction = nameof(VariableRestorer.RestoreAfterFunction);
	            yield return
	                new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression(_variableRestorerVariableName), restoreafterfunction), new NRefactory.PrimitiveExpression(statement.Variable.Value));
	            yield return ProcessAssignment(statement.Variable, Operator.Assignment, statement.Value);
	            yield break;
	        }

            yield return new NRefactory.VariableDeclarationStatement(LocalJamListAstType, ConverterLogic.CleanIllegalCharacters(statement.Variable.Value), new NRefactory.ObjectCreateExpression(LocalJamListAstType, ExpressionsForJamListConstruction(statement.Value)));
		}

        private bool IsInTopLevel(LocalStatement statement)
        {
            return FindParentOfType<RuleDeclarationStatement>(statement) == null;
        }

        private bool IsThisLocalOnlyUsedToNotMakeAnUpcomingForLoopNotWriteToGlobal(LocalStatement statement)
        {
            //if the initializer value has sideeffects, lets not throw away this localstatement
            if (statement.Value.Any(e => e is InvocationExpression))
                return false;

            var childrenAfterMe = statement.Parent.MyChildren.SkipWhile(n => n != statement).Skip(1);

            var expressionsReferringToThisLocal = childrenAfterMe.SelectMany(c => c.GetAllChildrenOfType<LiteralExpression>()).Where(l => l.Value == statement.Variable.Value);
            if (!expressionsReferringToThisLocal.Any())
                return false;

            return expressionsReferringToThisLocal.All(RefersToForLoopVariable);
        }

        private bool RefersToForLoopVariable(LiteralExpression arg)
        {
            return FindAllParentsOfType<ForStatement>(arg).Any(f => f.LoopVariable.Value == arg.Value);
        }

        private NRefactory.SwitchStatement ProcessSwitchStatement(SwitchStatement switchStatement)
        {
            var invocationExpression = new NRefactory.InvocationExpression(new NRefactory.IdentifierExpression("SwitchTokenFor"), ProcessExpression(switchStatement.Variable));
            var result = new NRefactory.SwitchStatement() {Expression = invocationExpression};
           
            foreach(var switchCase in switchStatement.Cases)
            {
                if (switchCase.CaseExpression.Value.Contains("*"))
                    throw new NotImplementedException("We dont support * yet in case");
                var section = new NRefactory.SwitchSection();
                section.CaseLabels.Add(new NRefactory.CaseLabel(new NRefactory.PrimitiveExpression(switchCase.CaseExpression.Value)));
                section.Statements.AddRange(switchCase.Statements.SelectMany(ProcessStatement));
                section.Statements.Add(new NRefactory.BreakStatement());
                result.SwitchSections.Add(section);
            }
            return result;
        }
        
        private NRefactory.ForeachStatement ProcessForStatement(ForStatement statement)
        {
            return new NRefactory.ForeachStatement
            {
                VariableType = LocalJamListAstType,
                VariableName = statement.LoopVariable.Value,
                InExpression = new NRefactory.MemberReferenceExpression(ProcessExpressionList(statement.List),"ElementsAsJamLists"),
                EmbeddedStatement = ProcessStatement(statement.Body).Single()
            };
        }

        private NRefactory.ExpressionStatement ProcessExpressionStatement(ExpressionStatement expressionStatement)
        {
            if (expressionStatement.Expression is InvocationExpression)
                return new NRefactory.ExpressionStatement(ProcessExpression(expressionStatement.Expression));
	
            throw new ArgumentException("Unsupported node: " + expressionStatement.Expression);
        }

	    private NRefactory.Statement ProcessAssignment(Expression left, Operator @operator, NodeList<Expression> right)
	    {
		    var csharpMethodNameForAssignmentOperator = CSharpMethodNameForAssignmentOperator(@operator);
		    var memberReferenceExpression = new NRefactory.MemberReferenceExpression(ProcessExpressionForLeftHandOfAssignment(left),
			    csharpMethodNameForAssignmentOperator);
		    var processExpression = ExpressionsForJamListConstruction(right);
		    return new NRefactory.InvocationExpression(memberReferenceExpression, processExpression);
	    }

	    private NRefactory.Expression ProcessExpressionForLeftHandOfAssignment(Expression left)
	    {
			//lefthandside:
			//mads = 2				         ->  Globals.mads.Assign("2");
			//mads_arg = 2				   ->  mads_arg.Assign("2");
			//$(mads) = 2					->  Globals.DereferenceElements(Globals.mads).Assign("2");
			//$($(mads)) = 2				->  Globals.DereferenceElements().DerefenceElements().Assign("2");
			//mads on mytarget = 2			->  Globals.GetOrCreateVariableOnTargetContext("mytarget", "mads").Assign(2);
			//$(mads) on mytarget = 2		->  Globals.GetOrCreateVariableOnTargetContext("mytarget", Globals.mads).Assign(2);
			//$(mads) on $(mytargets) = 2	->  Globals.GetOrCreateVariableOnTargetContext(Globals.mytargets, Globals.mads).Assign(2);
			var literalExpression = left as LiteralExpression;
		    if (literalExpression != null)
			    return ProcessIdentifier(left, literalExpression.Value);

		    var deref = left as VariableDereferenceExpression;
		    if (deref != null)
			    return DereferenceElementsNonFlatInvocationFor(ProcessExpressionForLeftHandOfAssignment(deref.VariableExpression));

		    var variableOnTargetExpression = left as VariableOnTargetExpression;
		    if (variableOnTargetExpression != null)
		    {
			    var variableExpression = ProcessExpression(variableOnTargetExpression.Variable);
			    var targetExpression = ProcessExpressionList(variableOnTargetExpression.Targets);
				return new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression("Globals"), "GetOrCreateVariableOnTargetContext"), targetExpression, variableExpression);
			}

		    var combineExpression = left as CombineExpression;
		    if (combineExpression != null)
			    return DereferenceElementsNonFlatInvocationFor(ProcessExpression(combineExpression));

		    throw new NotImplementedException();
	    }

	    private NRefactory.InvocationExpression DereferenceElementsNonFlatInvocationFor(NRefactory.Expression expression)
	    {
		    return new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression("Globals"), "DereferenceElementsNonFlat"), expression);
	    }

	    private NRefactory.Expression ProcessIdentifier(Node node, string identifierName)
	    {
		    var cleanName = ConverterLogic.CleanIllegalCharacters(identifierName);

		    if (node != null)
		    {
				var parentRule = FindParentOfType<RuleDeclarationStatement>(node);

				if (parentRule != null) 
				{
					var implicitIndex = GetImplicitVariableIndex (identifierName);
					if (implicitIndex != 0) {
						if (parentRule.Arguments.Length >= implicitIndex)
							return new NRefactory.IdentifierExpression (parentRule.Arguments [implicitIndex - 1]);
						else
							return new NRefactory.IdentifierExpression (GetImplicitVariableName (implicitIndex));
					}
					if (parentRule.Arguments.Contains (identifierName))
						return new NRefactory.IdentifierExpression (cleanName);
				}

		        bool identifierRefersToForLoopVariable = FindAllParentsOfType<ForStatement>(node).Any(f => f.LoopVariable.Value == identifierName);
		        bool identifierReferesToLocal = AllReachableLocals(node).Any(l => l.Variable.Value == identifierName && !IsInTopLevel(l));

		        if (identifierRefersToForLoopVariable || identifierReferesToLocal)
                    return new NRefactory.IdentifierExpression(cleanName);

		    }

		    return StaticGlobalVariableFor(identifierName);
	    }

        private IEnumerable<LocalStatement> AllReachableLocals(Node node)
        {
            if (node.Parent == null)
                yield break;

            var visibleSibblings = node.Parent.MyChildren.TakeWhile(sibbling => sibbling != node).AppendOne(node);
            
            foreach (var l in visibleSibblings.OfType<LocalStatement>())
                yield return l;

            if (node.Parent is RuleDeclarationStatement)
                yield break;

            foreach (var l in AllReachableLocals(node.Parent))
                yield return l;
        }

        private NRefactory.MemberReferenceExpression StaticGlobalVariableFor(string nonCleanName)
	    {
	        var cleanName = ConverterLogic.CleanIllegalCharacters(nonCleanName);
            if (_staticGlobals.Members.OfType<NRefactory.PropertyDeclaration>().All(p => p.Name != cleanName))
            {
                var indexerExpression = new NRefactory.IndexerExpression(new NRefactory.ThisReferenceExpression(), new NRefactory.PrimitiveExpression(nonCleanName));
                _staticGlobals.Members.Add(new NRefactory.PropertyDeclaration()
                {
                    Name = cleanName,
                    ReturnType = JamListBaseAstType,
                    Modifiers = NRefactory.Modifiers.Public,
                    Getter = new NRefactory.Accessor() {Body = new NRefactory.BlockStatement() {Statements = {new NRefactory.ReturnStatement(indexerExpression.Clone())}}},
                });
            }
            return new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression("Globals"), cleanName);
        }

        private IEnumerable<T> FindAllParentsOfType<T>(Node node) where T : Node
        {
            if (node.Parent == null)
                yield break;

            var needleNode = node.Parent as T;
            if (needleNode != null)
                yield return needleNode;

            foreach (var n in FindAllParentsOfType<T>(node.Parent))
                yield return n;
        }

        private T FindParentOfType<T>(Node node) where T : Node
        {
            return FindAllParentsOfType<T>(node).FirstOrDefault();
        }

        private static string CSharpMethodNameForAssignmentOperator(Operator assignmentOperator)
        {
            switch (assignmentOperator)
            {
				case Operator.Assignment:
		            return "Assign";
                case Operator.Append:
                    return "Append";
                case Operator.Subtract:
                    return "Subtract";
				case Operator.AssignmentIfEmpty:
		            return "AssignIfEmpty";
                default:
                    throw new NotSupportedException("Unsupported operator in assignment: " + assignmentOperator);
            }
        }

		static int GetImplicitVariableIndex(string variable)
		{
			if (variable.Length == 1) 
			{
				var ch = variable [0];
				if (ch >= '1' && ch <= '9')
					return ch - '0';
				else if (ch == '<')
					return 1;
				else if (ch == '>')
					return 2;
			}
			return 0;
		}

		static string GetImplicitVariableName(int index)
		{
			return "implicitArgument" + index;
		}

		static string[] SetupArgumentsFor(RuleDeclarationStatement ruleDeclaration)
		{
			//because the parser always interpets an invocation without any arguments as one with a single argument: an empty expressionlist,  let's make sure we always are ready to take a single argument
			var arguments = ruleDeclaration.Arguments;

			var variables = ruleDeclaration.GetAllChildrenOfType<VariableDereferenceExpression> ();

			foreach (var v in variables) 
			{
				var variable = v.VariableExpression as LiteralExpression;
				if (variable == null)
					continue;

				var implicitIndex = GetImplicitVariableIndex (variable.Value);
				if (implicitIndex != 0) 
				{
					if (arguments.Count () < implicitIndex) 
					{
						var newArguments = new string[implicitIndex];
						Array.Copy (arguments, newArguments, arguments.Length);
						for (int i = 0; i < implicitIndex; i++) 
						{
							if (newArguments[i] == null)
								newArguments[i] = GetImplicitVariableName (i + 1);
						}
						arguments = newArguments;
					}
				}
			}

			return arguments;
		}

        private NRefactory.ExpressionStatement ProcessRuleDeclarationStatement(RuleDeclarationStatement ruleDeclaration)
        {
			var arguments = SetupArgumentsFor (ruleDeclaration);

            var body = new NRefactory.BlockStatement();

			var methodName = MethodNameFor(ruleDeclaration);

            var processRuleDeclarationStatement = new NRefactory.MethodDeclaration()
            {
				Name = methodName,
                ReturnType = JamListBaseAstType,
                Modifiers = NRefactory.Modifiers.Static | NRefactory.Modifiers.Public,
                Body = body
            };

            var optionalArguments = FindOptionalArgumentsFor(ruleDeclaration);

            processRuleDeclarationStatement.Parameters.AddRange(arguments.Select(a =>
            {
	            var parameterDeclaration = new NRefactory.ParameterDeclaration(JamListBaseAstType, ArgumentNameFor(a));
				parameterDeclaration.DefaultExpression = optionalArguments.Contains(a) ? new NRefactory.NullReferenceExpression() : null;
	            return parameterDeclaration;
            }));

			//if (IsActions(methodName))
			//	body.Statements.Add (new NRefactory.InvocationExpression(new NRefactory.IdentifierExpression(ActionsNameFor(methodName)), arguments.Select(a => new NRefactory.IdentifierExpression(ArgumentNameFor(a)))));

            foreach (var arg in arguments.Where(a => IsArgumentModified(ruleDeclaration, a) || optionalArguments.Contains(a)))
	        {
				var identifier = new NRefactory.IdentifierExpression(ArgumentNameFor(arg));
		        var cloneExpression = new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(identifier, "Clone")); 
		        body.Statements.Add(new NRefactory.ExpressionStatement(new NRefactory.AssignmentExpression(identifier.Clone(), cloneExpression )));
	        }
			
           // body.Statements.Add(new NRefactory.IdentifierExpression($"System.Console.WriteLine(\"{ruleDeclaration.Name}\")"));

            foreach (var subStatement in ruleDeclaration.Body.Statements)
                body.Statements.AddRange(ProcessStatement(subStatement));

            if (!DoesBodyEndWithReturnStatement(ruleDeclaration))
                body.Statements.Add(new NRefactory.ReturnStatement(new NRefactory.ObjectCreateExpression(LocalJamListAstType)));
            
            typeForJamFile.Members.Add(processRuleDeclarationStatement);


			//do the registration

			var methodInfo = new NRefactory.IdentifierExpression ($"MethodBase.GetCurrentMethod().DeclaringType.GetMethod(nameof({methodName}))");

			return new NRefactory.ExpressionStatement(new NRefactory.InvocationExpression(new NRefactory.IdentifierExpression("RegisterRule"), new NRefactory.PrimitiveExpression(ruleDeclaration.Name), methodInfo));
        }

        bool IsArgumentModified(RuleDeclarationStatement rule, string argument)
        {
            return rule.GetAllChildrenOfType<AssignmentStatement>().Where(a => a.Left is LiteralExpression).Any(a => a.Left.As<LiteralExpression>().Value == argument);
        }

        private string[] FindOptionalArgumentsFor(RuleDeclarationStatement ruleDeclaration)
        {
            var invocationExpressions =
                _filesToTopLevel.Values.SelectMany(
                    t =>
                        t.GetAllChildrenOfType<InvocationExpression>()
                            .Where(i => i.RuleExpression is LiteralExpression)
                            .Where(i => i.RuleExpression.As<LiteralExpression>().Value == ruleDeclaration.Name)).ToArray();

            if (invocationExpressions.Length == 0)
                return new string[0];

            int allInvokersHaveAtLeastNArguments = invocationExpressions.Min(i => i.Arguments.Length);
            return ruleDeclaration.Arguments.Skip(allInvokersHaveAtLeastNArguments).ToArray();
        }

        private static bool DoesBodyEndWithReturnStatement(RuleDeclarationStatement ruleDeclaration)
	    {
		    var statements = ruleDeclaration.Body.Statements;
		    if (statements.Length == 0)
			    return false;
		    return statements.Last() is ReturnStatement;
	    }

	    public static NRefactory.AstType LocalJamListAstType => new NRefactory.SimpleType(nameof(LocalJamList));
		public static NRefactory.AstType JamListBaseAstType => new NRefactory.SimpleType(nameof(JamListBase));

		private NRefactory.IfElseStatement ProcessIfStatement(IfStatement ifStatement)
        {
            return new NRefactory.IfElseStatement(ProcessCondition(ifStatement.Condition), ProcessBlockStatement(ifStatement.Body), ProcessStatement(ifStatement.Else).SingleOrDefault());
        }
        
        private NRefactory.BlockStatement ProcessBlockStatement(BlockStatement blockStatement)
        {
            var processBlockStatement = new NRefactory.BlockStatement();
            processBlockStatement.Statements.AddRange(blockStatement.Statements.SelectMany(ProcessStatement));
            return processBlockStatement;
        }

        private NRefactory.WhileStatement ProcessWhileStatement(WhileStatement whileStatement)
        {
            return new NRefactory.WhileStatement(ProcessCondition(whileStatement.Condition), ProcessBlockStatement(whileStatement.Body));
        }

        private string ArgumentNameFor(string argumentName)
        {
            return ConverterLogic.CleanIllegalCharacters(argumentName);
        }

		private bool IsActions(string name)
		{
			return _allActions.Any(x => x.Name == name);
		}

		private bool IsRule(string name)
		{
			return _allRules.Any(x => x.Name == name);
		}
			
        private static string MethodNameFor(string ruleName)
        {
			return ConverterLogic.CleanIllegalCharacters(ruleName);
        }

		private static string ActionsNameFor(string name)
		{
			return "Actions."+name;
		}

        private static string MethodNameFor(RuleDeclarationStatement ruleDeclarationStatement)
        {
            return MethodNameFor(ruleDeclarationStatement.Name);
        }
    
        NRefactory.Expression ProcessCondition(Expression condition)
        {
         
	        var processExpression = ProcessExpression(condition);

            var literalExpression = condition as LiteralExpression;
            if (literalExpression != null)
                return new NRefactory.PrimitiveExpression(literalExpression.Value.Length != 0);
            
           return processExpression;
        }

	    string CSharpMethodForConditionOperator(Operator @operator)
        {
            switch (@operator)
            {
                case Operator.Assignment:
                    return "JamEquals";
                case Operator.In:
                    return "IsIn";
				case Operator.NotEqual:
		            return "NotJamEquals";
				case Operator.GreaterThan:
		            return "GreaterThan";
				case Operator.LessThan:
		            return "LessThan";
                default:
                    throw new NotSupportedException("Unknown conditional operator: "+@operator);
            }
        }

        public NRefactory.Expression ProcessExpressionList(NodeList<Expression> expressionList, bool mightModify = false)
        {
			
			if (expressionList.Length == 1 && mightModify)
	        {
		        if (expressionList[0] is VariableDereferenceExpression)
					return new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(ProcessExpression(expressionList[0]), "Clone"));
	        }

            var expressionsForJamListConstruction = ExpressionsForJamListConstruction(expressionList).ToArray();

            if (expressionsForJamListConstruction.Length == 1)
		        return expressionsForJamListConstruction[0];

            return new NRefactory.ObjectCreateExpression(LocalJamListAstType, expressionsForJamListConstruction);
        }

	    IEnumerable<NRefactory.Expression> ExpressionsForJamListConstruction(NodeList<Expression> expressionList)
        {
	        var result = expressionList.Select(e => ProcessExpression(e));

	        if (expressionList.OfType<AST.InvocationExpression>().Count() <= 1)
                return result;

	        NRefactory.Expression[] expressions = result.Reverse().ToArray();
	        return new [] { new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression(nameof(LocalJamList)), nameof(LocalJamList.CreateReversed)), expressions)};
        }

	    NRefactory.Expression ProcessExpression(Expression e, bool allowConversionToStringLiteral = true)
        {
			var literalExpression = e as LiteralExpression;
	        if (literalExpression != null)
	        {
	            var primitiveExpression = new NRefactory.PrimitiveExpression(literalExpression.Value);
	            return allowConversionToStringLiteral ? (NRefactory.Expression) primitiveExpression : new NRefactory.ObjectCreateExpression(LocalJamListAstType, primitiveExpression);
	        }

	        var expansionStyleExpression = e as ExpansionStyleExpression;
            if (expansionStyleExpression != null)
                return ProcessExpansionStyleExpression(expansionStyleExpression);

            var combineExpression = e as CombineExpression;
            if (combineExpression != null)
            {
                if (combineExpression.Elements.All(CanBePlacedInsideCSharpStringInterpolation))
                    return StringInterpolationFor(combineExpression);

                var combineMethod = new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression(nameof(JamListBase)), nameof(JamListBase.Combine));
                return new NRefactory.InvocationExpression(combineMethod, combineExpression.Elements.Select(e2=>ProcessExpression(e2)));
            }

            var invocationExpression = e as InvocationExpression;
            if (invocationExpression != null)
            {
	            return ProcessInvocationExpression(invocationExpression);
            }

		    var binaryOperatorExpression = e as BinaryOperatorExpression;
		    if (binaryOperatorExpression != null)
		    {
		        if (binaryOperatorExpression.Operator == Operator.And || binaryOperatorExpression.Operator == Operator.Or)
		        {
                    var left = ProcessExpression(binaryOperatorExpression.Left);
		            var right = ProcessExpressionList(binaryOperatorExpression.Right);
                    return new NRefactory.ParenthesizedExpression(new NRefactory.BinaryOperatorExpression(left, binaryOperatorExpression.Operator == Operator.And ? NRefactory.BinaryOperatorType.ConditionalAnd : NRefactory.BinaryOperatorType.ConditionalOr, right));
		        }

                var left2 = ProcessExpression(binaryOperatorExpression.Left, allowConversionToStringLiteral:false);
		        var right2 = ProcessExpressionList(binaryOperatorExpression.Right);

		        if (CanUseCustomIsIsOperator(binaryOperatorExpression, right2))
		            return new NRefactory.BinaryOperatorExpression(left2, NRefactory.BinaryOperatorType.Equality, right2);

		        return new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(left2, CSharpMethodForConditionOperator(binaryOperatorExpression.Operator)), right2);
		    }

		    var notOperatorExpression = e as NotOperatorExpression;
		    if (notOperatorExpression != null)
		    {
			    return new NRefactory.UnaryOperatorExpression(NRefactory.UnaryOperatorType.Not, new NRefactory.ParenthesizedExpression(ProcessExpression(notOperatorExpression.Expression)));
		    }

            if (e == null || e is EmptyExpression)
                return new NRefactory.ObjectCreateExpression(LocalJamListAstType);

            throw new NotImplementedException("CSharpFor cannot deal with " + e);
        }

        private bool CanUseCustomIsIsOperator(BinaryOperatorExpression binaryOperatorExpression, NRefactory.Expression right2)
        {
            if (binaryOperatorExpression.Operator != Operator.Assignment)
                return false;
            return right2 is NRefactory.PrimitiveExpression;
        }

        private NRefactory.Expression StringInterpolationFor(CombineExpression combineExpression)
        {
            var sb = new StringBuilder("$\"");
            foreach (var element in combineExpression.Elements)
                sb.Append(InterpolationValueFor(element));
            sb.Append("\"");
            return new NRefactory.IdentifierExpression(sb.ToString());
        }

        string InterpolationValueFor(Expression e)
        {
            var literal = e as LiteralExpression;
            if (literal != null)
                return literal.Value;

            var deref = e as VariableDereferenceExpression;
            if (deref != null)
            {
                var variableExpression = deref.VariableExpression;
                var literalValue = variableExpression as LiteralExpression;
                if (literalValue != null)
                {
                    if (IsGuaranteedToBeSingleItem(literalValue))
                        return "{" + ProcessExpression(e) + "}";
                    return null;
                }
               
            }
            return null;
        }

        private static bool IsGuaranteedToBeSingleItem(LiteralExpression literalValue)
        {
            switch (literalValue.Value)
            {
                case "TOP":
                case "PLATFORM":
                case "CONFIG":
                    return true;
                default:
                    return false;
            }
        }

        private bool CanBePlacedInsideCSharpStringInterpolation(Expression arg)
        {
            return InterpolationValueFor(arg) != null;
        }

        private NRefactory.Expression ProcessInvocationExpression(InvocationExpression invocationExpression)
        {
            var directInvocation = EmitAsCSharpCallIfPossible(invocationExpression);
            if (directInvocation != null)
                return directInvocation;
            
            var arguments = invocationExpression.Arguments.Select(a => ProcessExpressionList(a)).Prepend(ProcessExpression(invocationExpression.RuleExpression));
			return new NRefactory.InvocationExpression(new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression(nameof(DynamicRuleInvocationService)+".Instance"),"InvokeRule"),arguments);
		}

        private NRefactory.Expression EmitAsCSharpCallIfPossible(InvocationExpression invocationExpression)
        {
            var literalExpression = invocationExpression.RuleExpression as LiteralExpression;
            if (literalExpression == null)
                return null;

            if (_allActions.Any(a => a.Name == literalExpression.Value))
                return null;

            var matchingRules = _allRules.Where(r => r.Name == literalExpression.Value).ToArray();
            if (matchingRules.Length != 1)
                return null;

            var rule = matchingRules.Single();

            var methodName = MethodNameFor(invocationExpression.RuleExpression.As<LiteralExpression>().Value);

            var topLevelOfRule = FindParentOfType<TopLevel>(rule);

            var jamFileOfRule = _filesToTopLevel.Single(kvp => kvp.Value == topLevelOfRule).Key;

            var methodReference = topLevelOfRule == FindParentOfType<TopLevel>(invocationExpression)
                ? (NRefactory.Expression) new NRefactory.IdentifierExpression(methodName)
                : new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression(ConverterLogic.ClassNameForJamFile(jamFileOfRule.File)), methodName);

            var invocationArguments = invocationExpression.Arguments;

            if (rule.Arguments.Length == 0 && invocationArguments.Length == 1 && invocationArguments[0].Length == 0)
                invocationArguments = new NodeList<NodeList<Expression>>();

            return new NRefactory.InvocationExpression(methodReference, invocationArguments.Select(a => ProcessExpressionList(a)));
        }

        private NRefactory.Expression ProcessExpansionStyleExpression(ExpansionStyleExpression expansionStyleExpression)
        {
			//righthandside:
			//mads         ->   "mads"
			//$(mads);     ->   Globals.mads
			//$(mads_arg); ->   mads_arg
			//$($(mads));  ->   Globals.DereferenceElements(Globals.mads)
			//$($($(mads)));  ->   Globals.DereferenceElements(Globals.DereferenceElements(Globals.mads))
			//
			//@(mads)      ->   new LocalJamList("mads")
			//@(mads:S=.exe) -> new LocalJamList("mads").WithSuffix(".exe");

			var resultExpression = ProcessExpansionStyleExpressionVariablePreModifiers(expansionStyleExpression);
			
	        if (expansionStyleExpression.IndexerExpression != null)
            {
                var memberReferenceExpression = new NRefactory.MemberReferenceExpression(resultExpression, "IndexedBy");
                var indexerExpression = ProcessExpression(expansionStyleExpression.IndexerExpression);
                resultExpression = new NRefactory.InvocationExpression(memberReferenceExpression, indexerExpression);
            }


	        var variableDereferenceModifiers = new List<VariableDereferenceModifier>(expansionStyleExpression.Modifiers);

	        var sModfier = variableDereferenceModifiers.FirstOrDefault(v => v.Command == 'S' && v.Value == null);
            var bModfier = variableDereferenceModifiers.FirstOrDefault(v => v.Command == 'B' && v.Value == null);
	        bool hadSandB = false;
            if (sModfier != null && bModfier != null)
	        {
	            variableDereferenceModifiers.Remove(sModfier);
	            variableDereferenceModifiers.Remove(bModfier);
	            hadSandB = true;
	        }

            foreach (var modifier in variableDereferenceModifiers.OrderBy(m => JamModifierOrderFor(m.Command)))
            {
                var csharpMethod = CSharpMethodForModifier(modifier, modifier.Value != null);

                var memberReferenceExpression = new NRefactory.MemberReferenceExpression(resultExpression, csharpMethod);

                var args = modifier.Value == null ? new NRefactory.Expression[0] : new[] {ProcessExpression(modifier.Value)};
                resultExpression = new NRefactory.InvocationExpression(memberReferenceExpression, args);
            }

	        if (hadSandB)
	        {
                var memberReferenceExpression = new NRefactory.MemberReferenceExpression(resultExpression, "BaseAndSuffix");
                resultExpression = new NRefactory.InvocationExpression(memberReferenceExpression);
	        }
	        return resultExpression;
        }

        private int JamModifierOrderFor(char command)
        {
            const string order = "EWT  GRDBSM   UL/\\CJXI";
            return order.IndexOf(command);
        }

        private NRefactory.Expression ProcessExpansionStyleExpressionVariablePreModifiers(ExpansionStyleExpression expansionStyleExpression)
	    {
			if (expansionStyleExpression is LiteralExpansionExpression)
				return new NRefactory.ObjectCreateExpression(LocalJamListAstType, ProcessExpression(expansionStyleExpression.VariableExpression));
			
			//we know we are a variabledereferenceexpression now
		    expansionStyleExpression.As<VariableDereferenceExpression>();

			var literalExpression = expansionStyleExpression.VariableExpression as LiteralExpression;
		    if (literalExpression != null)
			    return ProcessIdentifier(literalExpression, literalExpression.Value);

		    return new NRefactory.InvocationExpression(
					    new NRefactory.MemberReferenceExpression(new NRefactory.IdentifierExpression("Globals"), "DereferenceElements"),
					    ProcessExpression(expansionStyleExpression.VariableExpression)
						);
	    }

	    private string CSharpMethodForModifier(VariableDereferenceModifier modifier, bool hasValue)
        {
            switch (modifier.Command)
            {
				case 'D':
		            return hasValue ? "SetDirectory" : "GetDirectory";
                case 'S':
                    return hasValue ? "WithSuffix" : "GetSuffix" ;
                case 'E':
                    return "IfEmptyUse";
                case 'G':
                    return hasValue ? "SetGrist" : "GetGrist";
                case 'J':
                    return hasValue ? "JoinWithValue" : "Join";
				case 'X':
		            return "Exclude";
				case 'I':
		            return "Include";
				case 'R':
		            return "Rooted";
				case 'P':
		            return "Parent";
				case 'U':
					return "ToUpper";
				case 'L':
					return "ToLower";
                case 'B':
                    return hasValue ? "SetBasePath" : "GetBasePath";
                case 'A':
                    if (hasValue) throw new NotSupportedException();
                    return "InterpetAsJamVariable";
                case 'W':
                    return "JamGlob";
                case 'T':
                    if (hasValue) throw new NotSupportedException();
                    return "GetBoundPath";
                case '\\':
                    if (hasValue) throw new NotSupportedException();
                    return "BackSlashify";
                case '/':
                    if (hasValue) throw new NotSupportedException();
                    return "ForwardSlashify";
                case 'C':
                    if (hasValue) throw new NotSupportedException();
                    return "Escape";
                default:
                    //return $"TodoModifier_{modifier.Command}"; 
                    throw new NotSupportedException("Unkown variable expansion command: " + modifier.Command);
            }
        }
    }
}
