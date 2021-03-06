#tool nuget:?package=NUnit.Runners&version=2.6.3
#addin nuget:?package=Cake.Xamarin

var TARGET = Argument ("target", Argument ("t", "Default"));

var ANDROID_DEVICES = (EnvironmentVariable ("ANDROID_DEVICES") ?? "").Split (';');
var IOS_DEVICES = (EnvironmentVariable ("IOS_DEVICES") ?? "").Split (';');
var NUNIT_PATH = GetFiles ("../packages/**/nunit.framework.dll").FirstOrDefault ();

Task ("Samples").Does (() =>
{
	EnsureDirectoryExists ("../output");

	var sampleSlns = new [] {
		"../ZXing.Net.Mobile.Mac.sln",
		"../ZXing.Net.Mobile.Forms.Mac.sln",		
		"../Samples/Android/Sample.Android.sln",
		"../Samples/iOS/Sample.iOS.sln",
		"../Samples/Forms/Sample.Forms.Mac.sln",
		"../ZXing.Net.Mobile.UITests.sln"
	};
	
	foreach (var sln in sampleSlns) {
		NuGetRestore (sln);
		DotNetBuild (sln, c => c.Configuration = "Debug");
	}
});

Task ("Android.UITests")
	.IsDependentOn ("Samples")
	.Does (() => 
{
	var uitests = "./Sample.Android.UITests/bin/Debug/Sample.Android.UITests.dll";

	var apk = AndroidPackage ("../Samples/Android/Sample.Android/Sample.Android.csproj", false, c => c.Configuration = "Release");
	Information ("APK: {0}", apk);

	foreach (var device in ANDROID_DEVICES) {
		System.Environment.SetEnvironmentVariable ("XTC_DEVICE_ID", device);
		Information ("Running Tests on: {0}", device);
		UITest (uitests, new NUnitSettings { ResultsFile = "../output/UITestResult-Android-" + device + ".xml" });
	}
});

Task ("iOS.UITests")
	.IsDependentOn ("Samples")
	.Does (() => 
{
	var uitests = "./Sample.iOS.UITests/bin/Debug/Sample.iOS.UITests.dll";

	DotNetBuild ("../Samples/iOS/Sample.iOS.sln", c => {
		c.Configuration = "Release";
		c.Properties ["Platform"] = new [] { "iPhone" }; 
	});

	foreach (var device in IOS_DEVICES) {
		System.Environment.SetEnvironmentVariable ("XTC_DEVICE_ID", device);
		Information ("Running Tests on: {0}", device);

		// Use ios-deploy app to install the iOS .app to device first
		// UITest requires it be already installed
		// To get ios-deploy:
		//   brew install node
		//   npm install -g ios-deploy
		StartProcess ("ios-deploy", new ProcessSettings {
			Arguments = string.Format ("--uninstall --id \"{0}\" --bundle \"{1}\"",
					device,
					"../Samples/iOS/Sample.iOS/bin/iPhone/Release/ZXingNetMobileiOSSample.app")
		});

		UITest (uitests, new NUnitSettings { ResultsFile = "../output/UITestResult-iOS-" + device + ".xml" });
	}
});

Task ("Forms.Android.UITests")
	.IsDependentOn ("Samples")
	.Does (() => 
{
	var uitests = "./FormsSample.UITests/bin/Debug/FormsSample.UITests.dll";

	var apk = AndroidPackage ("../Samples/Forms/Droid/FormsSample.Droid.csproj", false, c => c.Configuration = "Release");
	Information ("APK: {0}", apk);

	foreach (var device in ANDROID_DEVICES) {
		System.Environment.SetEnvironmentVariable ("XTC_DEVICE_ID", device);
		Information ("Running Tests on: {0}", device);
		UITest (uitests, new NUnitSettings { ResultsFile = "../output/UITestResult-FormsAndroid-" + device + ".xml" });
	}
});

Task ("Forms.iOS.UITests")
	.IsDependentOn ("Samples")
	.Does (() => 
{
	var uitests = "./FormsSample.UITests/bin/Debug/FormsSample.UITests.dll";

	DotNetBuild ("../Samples/Forms/iOS/FormsSample.iOS.csproj", c => {
		c.Configuration = "Release";
		c.Properties ["Platform"] = new [] { "iPhone" }; 
	});

	foreach (var device in IOS_DEVICES) {
		System.Environment.SetEnvironmentVariable ("XTC_DEVICE_ID", device);
		Information ("Running Tests on: {0}", device);

		// Use ios-deploy app to install the iOS .app to device first
		// UITest requires it be already installed
		// To get ios-deploy:
		//   brew install node
		//   npm install -g ios-deploy
		StartProcess ("ios-deploy", new ProcessSettings {
			Arguments = string.Format ("--uninstall --id \"{0}\" --bundle \"{1}\"",
					device,
					"../Samples/Forms/iOS/bin/iPhone/Release/FormsSampleiOS.app")
		});

		UITest (uitests, new NUnitSettings { ResultsFile = "../output/UITestResult-FormsiOS-" + device + ".xml" });
	}
});

Task ("All.UITests")
	.IsDependentOn ("Android.UITests")
	.IsDependentOn ("iOS.UITests")
	.IsDependentOn ("Forms.Android.UITests")
	.IsDependentOn ("Forms.iOS.UITests");

RunTarget (TARGET);