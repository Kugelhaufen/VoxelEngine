Run in editor with:
	Window -> General -> Test Runner
See resulst in editor with:
	Window -> Analysis -> Performance Test Report

Add "com.unity.test-framework.performance": "2.8.0-preview" (or a newer version) to manifest.json located in the "Packages" folder
https://docs.unity3d.com/Packages/com.unity.test-framework.performance@2.8/manual/index.html

How to use Performance Testing Extension for Unity Test Runner: 
https://docs.unity3d.com/Packages/com.unity.test-framework.performance@1.0/manual/index.html
https://blog.unity.com/technology/performance-benchmarking-in-unity-how-to-get-started

Run with command line (example):
Unity.exe -runTests [-batchmode] -projectPath <path> -testPlatform Android -buildTarget Android -playergraphicsapi=OpenGLES3 -mtRendering -scriptingbackend=mono -testResults <resultsPath> -logfile <logFilePath>