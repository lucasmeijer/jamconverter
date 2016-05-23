using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NiceIO;
using NUnit.Framework;
using Unity.IL2CPP;

namespace jamconverter.Tests
{
	[TestFixture]
	class Playground
	{
		[Test]
        //[Ignore("Playground")]
		public void A()
		{
			var converter = new JamToCSharpConverter();


            var convertfiles = new[] {"Projects/Jam/Editor.jam", "Projects/Jam/SetupRuntimeModules2.jam", "PlatformDependent/WinPlayer/StandalonePlayer.jam" };

            /*
			var files =
				convertfiles
					.Where(l => l[0] != '#' && !l.Contains("Config.jam"))
					.Select(fn => new NPath(fn));
                    */
            
			var basePath = new NPath("c:/unity");

		    var files = new List<NPath>();
            files.AddRange(basePath.Combine("Runtime").Files("*.jam", true));
            files.AddRange(basePath.Combine("Editor").Files("*.jam", true));
            files.AddRange(basePath.Combine("Projects/Jam").Files("*.jam", true));
            files.AddRange(basePath.Files("*.jam", false));
            files.AddRange(basePath.Combine("PlatformDependent").Files("*.jam", true));
            
			var program =
				files.Where(f=>!f.ToString().Contains("Config"))
					.Select(f => new SourceFileDescription() {File = f.RelativeTo(basePath), Contents = f.ReadAllText()})
					.ToArray();
            //var program = new[] {new SourceFileDescription() { Contents = new NPath(inputFile).ReadAllText(), FileName = "Main.cs"} };

            var csProgram = converter.Convert(new ProgramDescripton(program));
			
			
			var jambase = @"C:\jamconverter\external\jamplus\bin\Jambase.jam";
			var jamrunner = new JamRunner();

			var instructions = new JamRunnerInstructions()
			{
				CSharpFiles = csProgram,
				WorkingDir = new NPath("c:/unity"),
				JamFileToInvokeOnStartup = jambase.InQuotes(),
				AdditionalArg = "\"<StandalonePlayer>Runtime/Graphics/Texture2D.obj\" -sPLATFORM=win64 -q -dx"
            };

		    var tempDir = NPath.SystemTemp.Combine("JamRunner");
		    instructions.WorkingDir = instructions.WorkingDir ?? tempDir;

		    foreach (var f1 in instructions.JamfilesToCreate)
		        instructions.WorkingDir.Combine(f1.File).WriteAllText(f1.Contents);

		    string startupArg = "";
		    string csharparg = "";
		    if (instructions.CSharpFiles.Any())
		    {
		        var csharpExe = tempDir.Combine("csharp.exe");
		        CSharpRunner.Compile(instructions.CSharpFiles, JamToCSharpConverter.RuntimeDependencies, csharpExe);
		        csharparg = "-m " + csharpExe.InQuotes();
		    }

		    var jamPath = Environment.OSVersion.Platform == PlatformID.Win32NT ? "external/jamplus/bin/win32/jam.exe" : "external/jamplus/macosx64/jam";
		    var jamBinary = JamRunner.ConverterRoot.Combine(jamPath);

		    startupArg += "-f " + (instructions.JamFileToInvokeOnStartup ?? instructions.JamfilesToCreate[0].File.FileName);

		    startupArg += " -C " + instructions.WorkingDir;

		    startupArg += " -d +16" + instructions.AdditionalArg;
            

            var dropbox = new NPath(@"C:\Users\lucas\Dropbox");

		    var finalArg = jamBinary.ToString() + " "+startupArg + " " + csharparg+" "+instructions.AdditionalArg;
            Console.WriteLine("args: " + finalArg);

            var args2 = new Shell.ExecuteArgs() { Arguments = finalArg, Executable = jamBinary.ToString() };
		    var output_cs = dropbox.Combine("output_cs");
		    Shell.Execute(args2, null, output_cs);
            Truncate(output_cs);

            var args = new Shell.ExecuteArgs() { Arguments = startupArg + " " + instructions.AdditionalArg, Executable = jamBinary.ToString() };
		    var output_jam = dropbox.Combine("output_jam");
		    Shell.Execute(args, null, output_jam);
            Truncate(output_jam);

        }

	    private void Truncate(NPath outputJam)
	    {
	        var text = outputJam.ReadAllText();
	        var limit = 8000*1000;
	        if (text.Length > limit)
	            outputJam.WriteAllText(text.Substring(0, limit));
	    }

//local listIncludes = @(I=\\$(C.BUILD_EXTENSIONS)$:J=$(colon)) ;

        string OurJamFiles()
		{
			return 
			
@"Configuration\CombinedAssembliesDefines.jam
#Configuration\GlobalDefines.jam
Configuration\UnityEditorDefines.jam
Editor\Src\AI\AIEditorModule.jam
Editor\Src\CadImporter\CadImporter.jam
Editor\Src\GIS\GISEditorModule.jam
Editor\Src\NScreen\NScreenEditorModule.jam
Editor\Src\Physics\PhysicsEditor.jam
Editor\Src\SketchUp\SketchUpModule.jam
Editor\Src\TextRendering\TextRenderingEditor.jam
Editor\Src\VR\VREditorModule.jam
Extensions\Networking\UNetHLAPI.jam
Extensions\Networking\UNetWeaver.jam
Extensions\UnityAds\UnityAds.jam
Extensions\UnityAnalytics\UnityAnalytics.jam
Extensions\guisystem\guisystem.jam
External\Datakit\cadlib.jam
External\TextureCompressors\Crunch\libcrunch.jam
External\libjpeg-turbo\libjpeg.jam
External\libpng\libpng.jam
External\libwebsockets\libwebsockets.jam
External\pogostubs\pogostubslib.jam
External\udis86\udis86lib.jam
External\zlib\zlib.jam
External\zmq\zmqlib.jam
JamExtensions.jam
Jamfile.jam
Jamrules.jam
Pass1.jam
PlatformDependent\AndroidPlayer\Jam\AndroidPlayer.jam
#PlatformDependent\AndroidPlayer\Jam\Android_NDK.jam
PlatformDependent\AndroidPlayer\Jam\Android_SDK.jam
PlatformDependent\AndroidPlayer\Jam\Android_YASM.jam
PlatformDependent\AndroidPlayer\Jam\PlatformDefines.jam
PlatformDependent\AndroidPlayer\Jam\UnpackBuildsZip.jam
PlatformDependent\AndroidPlayer\Testing\AndroidAutomation.jam
PlatformDependent\BlackBerryPlayer\Jam\PlatformDefines.jam
PlatformDependent\BlackBerryPlayer\Testing\BlackBerryAutomation.jam
PlatformDependent\CommonWebPlayer\Jam\PlatformDefines.jam
PlatformDependent\CommonWebPlayer\Testing\WebAutomation.jam
PlatformDependent\LinuxStandalone\Jam\PlatformDefines.jam
PlatformDependent\LinuxStandalone\LinuxScreenSelector.jam
PlatformDependent\LinuxStandalone\LinuxStandaloneEditorExtensions.jam
PlatformDependent\LinuxStandalone\LinuxStandalonePlayer.jam
PlatformDependent\LinuxStandalone\Testing\LinuxStandaloneAutomation.jam
PlatformDependent\MetroPlayer\Jam\PlatformDefines.jam
PlatformDependent\MetroPlayer\Testing\MetroAutomation.jam
PlatformDependent\N3DS\Jam\Config.jam
PlatformDependent\N3DS\Jam\Jamfile.jam
PlatformDependent\N3DS\Jam\N3DSCgBatchPlugin.jam
PlatformDependent\N3DS\Jam\N3DSEditorExtensions.jam
PlatformDependent\N3DS\Jam\N3DSModule.jam
PlatformDependent\N3DS\Jam\N3DSPlayer.jam
PlatformDependent\N3DS\Jam\PlatformDefines.jam
PlatformDependent\N3DS\Jam\UnpackBuildsZip.jam
#PlatformDependent\N3DS\Jam\c-compilers\c-ctr.jam
#PlatformDependent\N3DS\Jam\c-compilers\ctr-armcc.jam
#PlatformDependent\N3DS\Jam\c-compilers\ctr-autodetect.jam
PlatformDependent\OSX\Jam\PlatformDefines.jam
PlatformDependent\OSXPlayer\MacStandaloneEditorExtensions.jam
PlatformDependent\OSXPlayer\MacStandalonePlayer.jam
PlatformDependent\OSXPlayer\Testing\OSXStandaloneAutomation.jam
PlatformDependent\PS3\Jam\Config.jam
PlatformDependent\PS3\Jam\Jamfile.jam
PlatformDependent\PS3\Jam\PS3CgBatchPlugin.jam
PlatformDependent\PS3\Jam\PS3EditorExtensions.jam
PlatformDependent\PS3\Jam\PS3Module.jam
PlatformDependent\PS3\Jam\PS3Player.jam
PlatformDependent\PS3\Jam\PlatformDefines.jam
PlatformDependent\PS3\Jam\SpuAudioMasterDSP.jam
PlatformDependent\PS3\Jam\SpuAudioMixerDuck.jam
PlatformDependent\PS3\Jam\SpuAudioMixerMetering.jam
PlatformDependent\PS3\Jam\SpuAudioMixerReceive.jam
PlatformDependent\PS3\Jam\SpuAudioMixerSend.jam
PlatformDependent\PS3\Jam\SpuGeomJob.jam
PlatformDependent\PS3\Jam\SpuSchedulerTask.jam
PlatformDependent\PS3\Jam\UnpackBuildsZip.jam
#PlatformDependent\PS3\Jam\c-compilers\c-ppu.jam
#PlatformDependent\PS3\Jam\c-compilers\c-spu.jam
#PlatformDependent\PS3\Jam\c-compilers\ps3-autodetect.jam
#PlatformDependent\PS3\Jam\c-compilers\ps3-vc.jam
PlatformDependent\PS3\Testing\PS3Automation.jam
PlatformDependent\PS4\Jam\Config.jam
PlatformDependent\PS4\Jam\Jamfile.jam
PlatformDependent\PS4\Jam\PS4Bdwgc.jam
PlatformDependent\PS4\Jam\PS4CgBatchPlugin.jam
PlatformDependent\PS4\Jam\PS4CompilerDefaults.jam
PlatformDependent\PS4\Jam\PS4EditorExtensions.jam
PlatformDependent\PS4\Jam\PS4Module.jam
PlatformDependent\PS4\Jam\PS4Player.jam
PlatformDependent\PS4\Jam\PlatformDefines.jam
PlatformDependent\PS4\Jam\UnpackBuildsZip.jam
#PlatformDependent\PS4\Jam\c-compilers\c-orbis.jam
#PlatformDependent\PS4\Jam\c-compilers\global-debugil2cpp.jam
#PlatformDependent\PS4\Jam\c-compilers\global-debugmono.jam
#PlatformDependent\PS4\Jam\c-compilers\global-masteril2cpp.jam
#PlatformDependent\PS4\Jam\c-compilers\global-mastermono.jam
#PlatformDependent\PS4\Jam\c-compilers\global-releaseil2cpp.jam
#PlatformDependent\PS4\Jam\c-compilers\global-releasemono.jam
#PlatformDependent\PS4\Jam\c-compilers\orbis-autodetect.jam
#PlatformDependent\PS4\Jam\c-compilers\orbis-vc.jam
PlatformDependent\PS4\Testing\PS4Automation.jam
PlatformDependent\PS4\Tools\il2cpp\PS4libil2cpp.jam
PlatformDependent\PSM\Jam\DevAssistant.jam
PlatformDependent\PSM\Jam\Jamfile.jam
PlatformDependent\PSM\Jam\PSMCgBatchPlugin.jam
PlatformDependent\PSM\Jam\PSMEditorExtensions.jam
PlatformDependent\PSM\Jam\PSMModule.jam
PlatformDependent\PSM\Jam\PSMPlayer.jam
PlatformDependent\PSM\Jam\PlatformDefines.jam
PlatformDependent\PSM\Jam\UnpackBuildsZip.jam
PlatformDependent\PSM\Testing\PSMAutomation.jam
PlatformDependent\PSP2Player\Jam\Config.jam
PlatformDependent\PSP2Player\Jam\Jamfile.jam
PlatformDependent\PSP2Player\Jam\PSP2CgBatchPlugin.jam
PlatformDependent\PSP2Player\Jam\PSP2EditorExtensions.jam
PlatformDependent\PSP2Player\Jam\PSP2Module.jam
PlatformDependent\PSP2Player\Jam\PSP2Player.jam
PlatformDependent\PSP2Player\Jam\PlatformDefines.jam
PlatformDependent\PSP2Player\Jam\UnpackBuildsZip.jam
#PlatformDependent\PSP2Player\Jam\c-compilers\c-psvita.jam
#PlatformDependent\PSP2Player\Jam\c-compilers\psvita-autodetect.jam
#PlatformDependent\PSP2Player\Jam\c-compilers\psvita-vc.jam
PlatformDependent\PSP2Player\Testing\PSP2Automation.jam
PlatformDependent\STVPlayer\Jam\Config.jam
PlatformDependent\STVPlayer\Jam\Jamfile.jam
PlatformDependent\STVPlayer\Jam\PlatformDefines.jam
PlatformDependent\STVPlayer\Jam\STVEditorExtensions.jam
PlatformDependent\STVPlayer\Jam\STVModule.jam
PlatformDependent\STVPlayer\Jam\STVPlayer.jam
#PlatformDependent\STVPlayer\Jam\c-compilers\STV_PREMIUM_13-autodetect.jam
#PlatformDependent\STVPlayer\Jam\c-compilers\STV_PREMIUM_13-gcc.jam
#PlatformDependent\STVPlayer\Jam\c-compilers\STV_PREMIUM_14-autodetect.jam
#PlatformDependent\STVPlayer\Jam\c-compilers\STV_PREMIUM_14-gcc.jam
#PlatformDependent\STVPlayer\Jam\c-compilers\STV_STANDARD_13-autodetect.jam
#PlatformDependent\STVPlayer\Jam\c-compilers\STV_STANDARD_13-gcc.jam
#PlatformDependent\STVPlayer\Jam\c-compilers\STV_STANDARD_14-autodetect.jam
#PlatformDependent\STVPlayer\Jam\c-compilers\STV_STANDARD_14-gcc.jam
#PlatformDependent\STVPlayer\Jam\c-compilers\STV_STANDARD_15-autodetect.jam
#PlatformDependent\STVPlayer\Jam\c-compilers\STV_STANDARD_15-gcc.jam
#PlatformDependent\STVPlayer\Testing\SamsungTVAutomation.jam
PlatformDependent\TizenPlayer\Jam\Config.jam
PlatformDependent\TizenPlayer\Jam\Jamfile.jam
PlatformDependent\TizenPlayer\Jam\PlatformDefines.jam
PlatformDependent\TizenPlayer\Jam\TizenEditorExtensions.jam
PlatformDependent\TizenPlayer\Jam\TizenPlayer.jam
PlatformDependent\TizenPlayer\Jam\UnpackBuildsZip.jam
#PlatformDependent\TizenPlayer\Jam\c-compilers\tizen-autodetect.jam
#PlatformDependent\TizenPlayer\Jam\c-compilers\tizen-gcc-debug.jam
#PlatformDependent\TizenPlayer\Jam\c-compilers\tizen-gcc-release.jam
#PlatformDependent\TizenPlayer\Jam\c-compilers\tizen-gcc.jam
PlatformDependent\TizenPlayer\Testing\TizenAutomation.jam
PlatformDependent\WP8Player\Jam\PlatformDefines.jam
PlatformDependent\WP8Player\Testing\WindowsPhone8Automation.jam
PlatformDependent\WebGL\Jam\Platform.jam
PlatformDependent\WebGL\Jam\PlatformDefines.jam
PlatformDependent\WebGL\Jam\SetupCompiler.jam
PlatformDependent\WebGL\Jam\UnpackBuildsZip.jam
PlatformDependent\WebGL\Jam\WebGLEditorExtensions.jam
PlatformDependent\WebGL\Jam\WebGLModule.jam
PlatformDependent\WebGL\Jam\WebGLSupport.jam
PlatformDependent\WebGL\Testing\WebGLAutomation.jam
PlatformDependent\WiiU\Jam\ArTools.jam
PlatformDependent\WiiU\Jam\Config.jam
PlatformDependent\WiiU\Jam\Jamfile.jam
PlatformDependent\WiiU\Jam\Platform.jam
PlatformDependent\WiiU\Jam\PlatformDefines.jam
PlatformDependent\WiiU\Jam\UnpackBuildsZip.jam
PlatformDependent\WiiU\Jam\WiiUCgBatchPlugin.jam
PlatformDependent\WiiU\Jam\WiiUEditorExtensions.jam
PlatformDependent\WiiU\Jam\WiiUExtVersionFileGenerator.jam
PlatformDependent\WiiU\Jam\WiiUInlineShader.jam
PlatformDependent\WiiU\Jam\WiiUModule.jam
PlatformDependent\WiiU\Jam\WiiUPlayer.jam
#PlatformDependent\WiiU\Jam\c-compilers\cafe-autodetect.jam
#PlatformDependent\WiiU\Jam\c-compilers\cafe-ghs-debug.jam
#PlatformDependent\WiiU\Jam\c-compilers\cafe-ghs-debugopt.jam
#PlatformDependent\WiiU\Jam\c-compilers\cafe-ghs-master.jam
#PlatformDependent\WiiU\Jam\c-compilers\cafe-ghs-release.jam
#PlatformDependent\WiiU\Jam\c-compilers\cafe-ghs.jam
PlatformDependent\WiiU\Testing\WiiUAutomation.jam
PlatformDependent\WiiU\Tests\CafeLibs.jam
PlatformDependent\WiiU\Tests\FP\Jamfile.jam
PlatformDependent\WiiU\Tests\HioHost\Config.jam
PlatformDependent\WiiU\Tests\HioHost\Jamfile.jam
PlatformDependent\WiiU\Tests\HioServer\Config.jam
PlatformDependent\WiiU\Tests\HioServer\Jamfile.jam
PlatformDependent\WiiU\Tests\Includes.jam
PlatformDependent\WiiU\Tests\SimpleLibProject\Config.jam
PlatformDependent\WiiU\Tests\SimpleLibProject\Jamfile.jam
PlatformDependent\WiiU\Tests\SimpleProject\Config.jam
PlatformDependent\WiiU\Tests\SimpleProject\Jamfile.jam
PlatformDependent\WiiU\Tests\Sqrt\Jamfile.jam
PlatformDependent\WiiU\Tests\StructFieldOffsets\Jamfile.jam
PlatformDependent\WiiU\Tests\SysInclude\Config.jam
PlatformDependent\WiiU\Tests\SysInclude\Jamfile.jam
PlatformDependent\WiiU\Tests\dlmalloc\Jamfile.jam
PlatformDependent\WiiU\Tools\CafeShaderCompiler\jamfile.jam
PlatformDependent\Win\Jam\PlatformDefines.jam
PlatformDependent\WinPlayer\StandalonePlayer.jam
PlatformDependent\WinPlayer\Testing\WindowsStandaloneAutomation.jam
PlatformDependent\WinPlayer\WindowsStandaloneEditorExtensions.jam
PlatformDependent\WinRT\AssemblyConverter\AssemblyConverter.jam
PlatformDependent\WinRT\InternalCallReplacer\InternalCallReplacer.jam
PlatformDependent\WinRT\InternalCallReplacer2\InternalCallReplacer2.jam
#PlatformDependent\WinRT\WinRT.jam
PlatformDependent\Xbox360\External\bdwgc\Xbox360Bdwgc.jam
PlatformDependent\Xbox360\Jam\PlatformDefines.jam
PlatformDependent\Xbox360\Jam\UnpackBuildsZip.jam
PlatformDependent\Xbox360\Jam\Xbox360CgBatchPlugin.jam
PlatformDependent\Xbox360\Jam\Xbox360CompilerDefaults.jam
PlatformDependent\Xbox360\Jam\Xbox360EditorExtensions.jam
PlatformDependent\Xbox360\Jam\Xbox360Module.jam
PlatformDependent\Xbox360\Jam\Xbox360Player.jam
PlatformDependent\Xbox360\Projects\Config.jam
PlatformDependent\Xbox360\Projects\Jamfile.jam
PlatformDependent\Xbox360\Projects\c-compilers\xbox360-autodetect.jam
PlatformDependent\Xbox360\Projects\c-compilers\xbox360-debug-vc.jam
PlatformDependent\Xbox360\Projects\c-compilers\xbox360-release-vc.jam
PlatformDependent\Xbox360\Projects\c-compilers\xbox360-releaseltcg-vc.jam
PlatformDependent\Xbox360\Projects\c-compilers\xbox360-vc.jam
PlatformDependent\Xbox360\Testing\Xbox360Automation.jam
PlatformDependent\Xbox360\Tools\il2cpp\Xbox360libil2cpp.jam
PlatformDependent\XboxOne\External\bdwgc\XboxOneBdwgc.jam
PlatformDependent\XboxOne\Jam\Config.jam
PlatformDependent\XboxOne\Jam\Jamfile.jam
PlatformDependent\XboxOne\Jam\PlatformDefines.jam
PlatformDependent\XboxOne\Jam\UnpackBuildsZip.jam
PlatformDependent\XboxOne\Jam\XboxOneCgBatchPlugin.jam
PlatformDependent\XboxOne\Jam\XboxOneCompilerDefaults.jam
PlatformDependent\XboxOne\Jam\XboxOneEditorExtensions.jam
PlatformDependent\XboxOne\Jam\XboxOneModule.jam
PlatformDependent\XboxOne\Jam\XboxOnePlayer.jam
#PlatformDependent\XboxOne\Jam\c-compilers\durango-autodetect.jam
#PlatformDependent\XboxOne\Jam\c-compilers\durango-c-vc-autodetect.jam
#PlatformDependent\XboxOne\Jam\c-compilers\durango-c-vc-crt-defines.jam
#PlatformDependent\XboxOne\Jam\c-compilers\durango-c-vc.jam
#PlatformDependent\XboxOne\Jam\c-compilers\durango-vc-debug.jam
#PlatformDependent\XboxOne\Jam\c-compilers\durango-vc-master.jam
#PlatformDependent\XboxOne\Jam\c-compilers\durango-vc-release.jam
#PlatformDependent\XboxOne\Jam\c-compilers\durango-vc.jam
PlatformDependent\XboxOne\Testing\XboxOneAutomation.jam
#PlatformDependent\XboxOne\Tools\il2cpp\XboxOnelibil2cpp.jam
PlatformDependent\iPhonePlayer\Jam\PlatformDefines.jam
PlatformDependent\iPhonePlayer\Jam\UnpackBuildsZip.jam
PlatformDependent\iPhonePlayer\Testing\iPhoneAutomation.jam
Projects\Jam\BB10EditorExtensions.jam
Projects\Jam\BB10Export.jam
Projects\Jam\BaseJamIncludes.jam
Projects\Jam\Binary2Text.jam
Projects\Jam\BlackBerryPlayer.jam
Projects\Jam\BuiltinResources.jam
Projects\Jam\CSharpSupport\CSharpSupport.jam
Projects\Jam\CSharpSupport\Example\Example.jam
Projects\Jam\CgBatch.jam
Projects\Jam\CombinedAssemblies.jam
Projects\Jam\DocBrowserModel.jam
Projects\Jam\DocCombiner.jam
Projects\Jam\DocCommon.jam
Projects\Jam\DocGen.jam
Projects\Jam\Editor.jam
Projects\Jam\EditorCrashHandlerLib.jam
Projects\Jam\EditorFiles.jam
Projects\Jam\EditorLogicGraph.jam
Projects\Jam\GenerateUnityConfigure.jam
Projects\Jam\GeometryToolbox.jam
Projects\Jam\GlslOptimizer.jam
Projects\Jam\GradleIntegration.jam
Projects\Jam\HLSLcc.jam
Projects\Jam\ImportFBX.jam
Projects\Jam\JobProcess.jam
Projects\Jam\LinuxEditor.jam
Projects\Jam\LinuxWebPlugin.jam
Projects\Jam\MacEditor.jam
Projects\Jam\ManagedProjectFiles.jam
Projects\Jam\MetroEditorExtensions.jam
Projects\Jam\MetroSupport.jam
Projects\Jam\NasmJamrules.jam
Projects\Jam\NativeUnitTests.jam
Projects\Jam\NonLumpableFiles.jam
Projects\Jam\PackageManager.jam
Projects\Jam\QuicktimeTools.jam
Projects\Jam\Rules\BuildHost\BuildPlatform.jam
Projects\Jam\Rules\BuildHost\HostLINUX.jam
Projects\Jam\Rules\BuildHost\HostMACOSX.jam
Projects\Jam\Rules\BuildHost\HostNT.jam
Projects\Jam\Rules\IvyRules.jam
Projects\Jam\Rules\TargetPlatformRules.jam
Projects\Jam\Rules\UnityRules.jam
Projects\Jam\RuntimeFiles.jam
Projects\Jam\SetupRuntimeModules2.jam
Projects\Jam\UNetServerLib.jam
Projects\Jam\UnitTest++.jam
Projects\Jam\UnityAsmUtilsLib.jam
Projects\Jam\UnityYAMLMerge.jam
Projects\Jam\UnityYAMLMergeLib.jam
Projects\Jam\UnpackBuildsZip.jam
Projects\Jam\Unwrap.jam
Projects\Jam\WP8EditorExtensions.jam
Projects\Jam\WP8Support.jam
Projects\Jam\WebPlayer.jam
Projects\Jam\WorkSpaceGenerationConfig.jam
Projects\Jam\WorkSpaceGenerationConfigWin.jam
Projects\Jam\hlslang.jam
Projects\Jam\iOSCompilerSetup.jam
Projects\Jam\iOSEditorExtensions.jam
Projects\Jam\iOSPlayer.jam
Projects\Jam\iOSSupport.jam
Projects\Metro\Config.jam
Projects\Metro\Jamfile.jam
Projects\Metro81\Config.jam
Projects\Metro81\Jamfile.jam
Projects\UAP\Config.jam
Projects\UAP\Jamfile.jam
Projects\WP8.1\Config.jam
Projects\WP8.1\Jamfile.jam
Projects\WP8\Config.jam
Projects\WP8\Jamfile.jam
Runtime\AI\AIModule.jam
Runtime\Animation\AnimationModule.jam
Runtime\Audio\AudioModule.jam
Runtime\CloudWebServices\CloudWebServicesModule.jam
Runtime\ClusterInput\ClusterInputModule.jam
Runtime\ClusterRenderer\ClusterRendererModule.jam
Runtime\Dynamics\PhysicsModule.jam
Runtime\IMGUI\IMGUIModule.jam
Runtime\Managed\CrossDomainPolicyParser\CrossDomainPolicyParser.jam
Runtime\NScreen\NScreenModule.jam
Runtime\Networking\UNETModule.jam
Runtime\ParticleSystem\ParticleSystemModule.jam
Runtime\ParticlesLegacy\ParticlesLegacyModule.jam
Runtime\Physics2D\Physics2DModule.jam
Runtime\Terrain\TerrainModule.jam
Runtime\TerrainPhysics\TerrainPhysicsModule.jam
Runtime\TextRendering\TextRenderingModule.jam
Runtime\UI\UIModule.jam
Runtime\Umbra\UmbraModule.jam
Runtime\UnityAds\UnityAdsModule.jam
Runtime\UnityAnalytics\UnityAnalyticsModule.jam
Runtime\VR\VRModule.jam
Runtime\Web\WebModule.jam
Runtime\WebRequest\WebRequestModule.jam
Tools\AssemblyPatcher\AssemblyPatcher.jam
Tools\BugReporterV2\BugReporter.jam
Tools\BugReporterV2\Jamfile.jam
Tools\BugReporterV2\Jamrules.jam
Tools\BugReporterV2\attachment\Jamfile.jam
Tools\BugReporterV2\attachment\lib\Jamfile.jam
Tools\BugReporterV2\attachment\tests\Jamfile.jam
Tools\BugReporterV2\common\Jamfile.jam
Tools\BugReporterV2\common\tests\Jamfile.jam
Tools\BugReporterV2\file_system\Jamfile.jam
Tools\BugReporterV2\file_system\lib\Jamfile.jam
Tools\BugReporterV2\file_system\tests\Jamfile.jam
Tools\BugReporterV2\google-search\Jamfile.jam
Tools\BugReporterV2\google-search\lib\Jamfile.jam
Tools\BugReporterV2\jam_modules\InformationPropertyList.jam
Tools\BugReporterV2\jam_modules\qt5.jam
Tools\BugReporterV2\jam_modules\test.jam
Tools\BugReporterV2\launcher\Jamfile.jam
Tools\BugReporterV2\launcher\app\Jamfile.jam
Tools\BugReporterV2\launcher\tests\Jamfile.jam
Tools\BugReporterV2\long_term_operation\Jamfile.jam
Tools\BugReporterV2\long_term_operation\lib\Jamfile.jam
Tools\BugReporterV2\long_term_operation\tests\Jamfile.jam
Tools\BugReporterV2\macx_collectors\Jamfile.jam
Tools\BugReporterV2\macx_collectors\lib\Jamfile.jam
Tools\BugReporterV2\macx_collectors\tests\Jamfile.jam
Tools\BugReporterV2\progression\Jamfile.jam
Tools\BugReporterV2\progression\lib\Jamfile.jam
Tools\BugReporterV2\progression\tests\Jamfile.jam
Tools\BugReporterV2\qt_components\Jamfile.jam
Tools\BugReporterV2\qt_face\Jamfile.jam
Tools\BugReporterV2\qt_face\lib\Jamfile.jam
Tools\BugReporterV2\qt_face\tests\Jamfile.jam
Tools\BugReporterV2\reporter\Jamfile.jam
Tools\BugReporterV2\reporter\lib\Jamfile.jam
Tools\BugReporterV2\reporter\tests\Jamfile.jam
Tools\BugReporterV2\search\Jamfile.jam
Tools\BugReporterV2\search\lib\Jamfile.jam
Tools\BugReporterV2\search_integration\Jamfile.jam
Tools\BugReporterV2\search_integration\lib\Jamfile.jam
Tools\BugReporterV2\search_integration\tests\Jamfile.jam
Tools\BugReporterV2\sender\Jamfile.jam
Tools\BugReporterV2\sender\lib\Jamfile.jam
Tools\BugReporterV2\sender\tests\Jamfile.jam
Tools\BugReporterV2\settings\Jamfile.jam
Tools\BugReporterV2\settings\lib\Jamfile.jam
Tools\BugReporterV2\static_properties\Jamfile.jam
Tools\BugReporterV2\static_properties\app\Jamfile.jam
Tools\BugReporterV2\static_properties\lib\Jamfile.jam
Tools\BugReporterV2\sysinfo\Jamfile.jam
Tools\BugReporterV2\sysinfo\lib\Jamfile.jam
Tools\BugReporterV2\sysinfo\tests\Jamfile.jam
Tools\BugReporterV2\system_collectors\Jamfile.jam
Tools\BugReporterV2\system_collectors\lib\Jamfile.jam
Tools\BugReporterV2\system_collectors\tests\Jamfile.jam
Tools\BugReporterV2\system_interplay\Jamfile.jam
Tools\BugReporterV2\system_interplay\lib\Jamfile.jam
Tools\BugReporterV2\system_interplay\tests\Jamfile.jam
Tools\BugReporterV2\test_ui\Jamfile.jam
Tools\BugReporterV2\test_ui\app\Jamfile.jam
Tools\BugReporterV2\testing\Jamfile.jam
Tools\BugReporterV2\testing\lib\Jamfile.jam
Tools\BugReporterV2\testing\tests\Jamfile.jam
Tools\BugReporterV2\unittest++\Jamfile.jam
Tools\BugReporterV2\unity_collectors\Jamfile.jam
Tools\BugReporterV2\unity_collectors\lib\Jamfile.jam
Tools\BugReporterV2\unity_collectors\tests\Jamfile.jam
Tools\BugReporterV2\unity_version\Jamfile.jam
Tools\BugReporterV2\unity_version\lib\Jamfile.jam
Tools\BugReporterV2\win_collectors\Jamfile.jam
Tools\BugReporterV2\win_collectors\lib\Jamfile.jam
Tools\BugReporterV2\win_collectors\tests\Jamfile.jam
Tools\BugReporterV2\zip_packer\Jamfile.jam
Tools\BugReporterV2\zip_packer\lib\Jamfile.jam
Tools\BugReporterV2\zip_packer\tests\Jamfile.jam
Tools\BugReporterWin\lib\CrashHandlerLib.jam
Tools\InternalCallRegistrationWriter\InternalCallRegistrationWriter.jam
Tools\ScriptUpdater\ScriptUpdater.jam
Tools\SerializationWeaver\SerializationWeaver.jam
Tools\ThreadMixer\win32\Jamfile.jam
Tools\ThreadMixer\win32\common\Jamfile.jam
Tools\ThreadMixer\win32\injector\Jamfile.jam
Tools\ThreadMixer\win32\interceptor\Jamfile.jam
Tools\ThreadMixer\win32\starter\Jamfile.jam
Tools\UnityBindingsParser\BindingsToCsAndCpp\BindingsToCsAndCpp.jam
Tools\UnusedByteCodeStripper\UnusedBytecodeStripper.jam
Tools\UnusedByteCodeStripper2\UnusedBytecodeStripper2.jam
Tools\WinUtils\EnableAttachDebugger\EnableAttachDebugger.jam
Tools\il2cpp\IL2CPP.jam
Tools\il2cpp\MapFileParser.jam
Tools\il2cpp\il2cpp\external\boehmgc\bdwgc.jam
Tools\il2cpp\libil2cpp.jam";
		}

	}
	
}
