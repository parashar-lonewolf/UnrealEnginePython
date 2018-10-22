// Copyright 1998-2016 Epic Games, Inc. All Rights Reserved.

using UnrealBuildTool;
using System.IO;
using System.Collections.Generic;
//using Tools.DotNETCommon;

public class UnrealEnginePython : ModuleRules
{

    // leave this string as empty for triggering auto-discovery of python installations...
    private string PythonHome = "";
    bool UseThirdPartyPython = true;    //embedded
	bool AutoAddProjectScriptsInPackaging = true;

	//Or use the one included in third party folder
	protected string ThirdPartyPythonHome
    {
        get
        {
            return Path.GetFullPath(Path.Combine(ThirdPartyPath, PythonType));
        }
    }



    // otherwise specify the path of your python installation
    //private string PythonHome = "C:/Program Files/Python36";
    // this is an example for Homebrew on Mac
    //private string PythonHome = "/usr/local/Cellar/python3/3.6.0/Frameworks/Python.framework/Versions/3.6/";
    // on Linux an include;libs syntax is expected:
    //private string PythonHome = "/usr/local/include/python3.6;/usr/local/lib/libpython3.6.so"

    //Swap python versions here
    private string PythonType = "Python36";
    //private string PythonType = "python27";

    private string ThirdPartyPath
    {
        get { return Path.GetFullPath(Path.Combine(ModuleDirectory, "../../ThirdParty/")); }
    }

	private string BinariesPath
	{
		get { return Path.GetFullPath(Path.Combine(ModuleDirectory, "../../Binaries/")); }
	}

	private string ScriptsPath
	{
		get { return Path.GetFullPath(Path.Combine(ModuleDirectory, "../../Content/Scripts/")); }
	}

	public string GetUProjectPath()
	{
		return Path.Combine(ModuleDirectory, "../../../..");
	}

	private void CopyToProjectBinaries(string Filepath, ReadOnlyTargetRules Target)
	{
		//System.Console.WriteLine("uprojectpath is: " + Path.GetFullPath(GetUProjectPath()));

		string BinariesDir = Path.Combine(GetUProjectPath(), "Binaries", Target.Platform.ToString());
		string Filename = Path.GetFileName(Filepath);

		//convert relative path 
		string FullBinariesDir = Path.GetFullPath(BinariesDir);

		if (!Directory.Exists(FullBinariesDir))
		{
			Directory.CreateDirectory(FullBinariesDir);
		}

		string FullExistingPath = Path.Combine(FullBinariesDir, Filename);
		bool ValidFile = false;

		//File exists, check if they're the same
		if (File.Exists(FullExistingPath))
		{
			ValidFile = true;
		}

		//No valid existing file found, copy new dll
		if(!ValidFile)
		{
			File.Copy(Filepath, Path.Combine(FullBinariesDir, Filename), true);
		}
	}


	public void AddRuntimeDependenciesForCopying(ReadOnlyTargetRules Target)
	{
		if ((Target.Platform == UnrealTargetPlatform.Win64) || (Target.Platform == UnrealTargetPlatform.Win32))
		{
			RuntimeDependencies.Add(Path.Combine(ScriptsPath, "..."));

			if(AutoAddProjectScriptsInPackaging)
			{
				RuntimeDependencies.Add("$(ProjectDir)/Content/Scripts/...");
			}
			//Copy binaries as dependencies
			if(UseThirdPartyPython)
			{
				string PlatformString = Target.Platform.ToString();
				bool bVerboseBuild = false;

				//Don't add android stuff so we use a manual method to enum a directory
				Tools.DotNETCommon.DirectoryReference BinDir = new Tools.DotNETCommon.DirectoryReference(Path.Combine(BinariesPath, PlatformString, "..."));

				if (Tools.DotNETCommon.DirectoryReference.Exists(BinDir))
				{
					foreach (Tools.DotNETCommon.FileReference File in Tools.DotNETCommon.DirectoryReference.EnumerateFiles(BinDir, "*", SearchOption.AllDirectories))
					{
						//if (!File.ToString().Contains("android"))
						bool bValidFile = !File.ToString().Contains("Lib\\site-packages");	//don't copy site-packages (staging path likely too long) 
						bValidFile = bValidFile && !File.ToString().Contains("Binaries\\Win64\\UE4Editor");	//don't copy UEEditor dll/pdbs. These are not used

						if (bValidFile)
						{
							RuntimeDependencies.Add(File.ToString());
						}
						else if (bVerboseBuild)
						{
							Log.TraceInformation("Not adding the following file as RuntimeDependency: ");
							Log.TraceInformation(File.ToString());
						}
					}
				}

				//Copy the thirdparty dll so ue4 loads it by default
				string DLLName = PythonType.ToLower() + ".dll";
				string PluginDLLPath = Path.Combine(BinariesPath, PlatformString, DLLName);
				CopyToProjectBinaries(PluginDLLPath, Target);

				//After it's been copied add it as a dependency so it gets copied on packaging
				string DLLPath = Path.GetFullPath(Path.Combine(GetUProjectPath(), "Binaries", PlatformString, DLLName));
				RuntimeDependencies.Add(DLLPath);

			}//end if thirdparty
		}//end windows
	}

	private string[] windowsKnownPaths =
    {
        "C:/Program Files/Python37",
        "C:/Program Files/Python36",
        "C:/Program Files/Python35",
        "C:/Python27",
        "C:/IntelPython35"
    };

    private string[] macKnownPaths =
    {
        "/Library/Frameworks/Python.framework/Versions/3.7",
        "/Library/Frameworks/Python.framework/Versions/3.6",
        "/Library/Frameworks/Python.framework/Versions/3.5",
        "/Library/Frameworks/Python.framework/Versions/2.7",
        "/System/Library/Frameworks/Python.framework/Versions/3.7",
        "/System/Library/Frameworks/Python.framework/Versions/3.6",
        "/System/Library/Frameworks/Python.framework/Versions/3.5",
        "/System/Library/Frameworks/Python.framework/Versions/2.7"
    };

    private string[] linuxKnownIncludesPaths =
    {
        "/usr/local/include/python3.7",
        "/usr/local/include/python3.7m",
        "/usr/local/include/python3.6",
        "/usr/local/include/python3.6m",
        "/usr/local/include/python3.5",
        "/usr/local/include/python3.5m",
        "/usr/local/include/python2.7",
        "/usr/include/python3.7",
        "/usr/include/python3.7m",
        "/usr/include/python3.6",
        "/usr/include/python3.6m",
        "/usr/include/python3.5",
        "/usr/include/python3.5m",
        "/usr/include/python2.7",
    };

    private string[] linuxKnownLibsPaths =
    {
        "/usr/local/lib/libpython3.7.so",
        "/usr/local/lib/libpython3.7m.so",
        "/usr/local/lib/x86_64-linux-gnu/libpython3.7.so",
        "/usr/local/lib/x86_64-linux-gnu/libpython3.7m.so",
        "/usr/local/lib/libpython3.6.so",
        "/usr/local/lib/libpython3.6m.so",
        "/usr/local/lib/x86_64-linux-gnu/libpython3.6.so",
        "/usr/local/lib/x86_64-linux-gnu/libpython3.6m.so",
        "/usr/local/lib/libpython3.5.so",
        "/usr/local/lib/libpython3.5m.so",
        "/usr/local/lib/x86_64-linux-gnu/libpython3.5.so",
        "/usr/local/lib/x86_64-linux-gnu/libpython3.5m.so",
        "/usr/local/lib/libpython2.7.so",
        "/usr/local/lib/x86_64-linux-gnu/libpython2.7.so",
        "/usr/lib/libpython3.7.so",
        "/usr/lib/libpython3.7m.so",
        "/usr/lib/x86_64-linux-gnu/libpython3.7.so",
        "/usr/lib/x86_64-linux-gnu/libpython3.7m.so",
        "/usr/lib/libpython3.6.so",
        "/usr/lib/libpython3.6m.so",
        "/usr/lib/x86_64-linux-gnu/libpython3.6.so",
        "/usr/lib/x86_64-linux-gnu/libpython3.6m.so",
        "/usr/lib/libpython3.5.so",
        "/usr/lib/libpython3.5m.so",
        "/usr/lib/x86_64-linux-gnu/libpython3.5.so",
        "/usr/lib/x86_64-linux-gnu/libpython3.5m.so",
        "/usr/lib/libpython2.7.so",
        "/usr/lib/x86_64-linux-gnu/libpython2.7.so",
    };

#if WITH_FORWARDED_MODULE_RULES_CTOR
    public UnrealEnginePython(ReadOnlyTargetRules Target) : base(Target)
#else
    public UnrealEnginePython(TargetInfo Target)
#endif
    {

        PCHUsage = ModuleRules.PCHUsageMode.UseExplicitOrSharedPCHs;
        string enableUnityBuild = System.Environment.GetEnvironmentVariable("UEP_ENABLE_UNITY_BUILD");
        bFasterWithoutUnity = string.IsNullOrEmpty(enableUnityBuild);

        PublicIncludePaths.AddRange(
            new string[] {
                "UnrealEnginePython/Public",
				// ... add public include paths required here ...
            }
            );


        PrivateIncludePaths.AddRange(
            new string[] {
                "UnrealEnginePython/Private",
                PythonHome,
				// ... add other private include paths required here ...
			}
            );


        PublicDependencyModuleNames.AddRange(
            new string[]
            {
                "Core",
                "Sockets",
                "Networking",
                "Projects"
				// ... add other public dependencies that you statically link with here ...
			}
            );


        PrivateDependencyModuleNames.AddRange(
            new string[]
            {
                "CoreUObject",
                "Engine",
                "InputCore",
                "Slate",
                "SlateCore",
                "MovieScene",
                "LevelSequence",
                "HTTP",
                "UMG",
                "AppFramework",
                "RHI",
                "Voice",
                "RenderCore",
                "MovieSceneCapture",
                "Landscape",
                "Foliage",
                "AIModule"
				// ... add private dependencies that you statically link with here ...
			}
            );


#if WITH_FORWARDED_MODULE_RULES_CTOR
        BuildVersion Version;
        if (BuildVersion.TryRead(BuildVersion.GetDefaultFileName(), out Version))
        {
            if (Version.MinorVersion >= 18)
            {
                PrivateDependencyModuleNames.Add("ApplicationCore");
            }
        }
#endif


        DynamicallyLoadedModuleNames.AddRange(
            new string[]
            {
				// ... add any modules that your module loads dynamically here ...
			}
            );

#if WITH_FORWARDED_MODULE_RULES_CTOR
        if (Target.bBuildEditor)
#else
        if (UEBuildConfiguration.bBuildEditor)
#endif
        {
            PrivateDependencyModuleNames.AddRange(new string[]{
                "UnrealEd",
                "LevelEditor",
                "BlueprintGraph",
                "Projects",
                "Sequencer",
                "SequencerWidgets",
                "AssetTools",
                "LevelSequenceEditor",
                "MovieSceneTools",
                "MovieSceneTracks",
                "CinematicCamera",
                "EditorStyle",
                "GraphEditor",
                "UMGEditor",
                "AIGraph",
                "RawMesh",
                "DesktopWidgets",
                "EditorWidgets",
                "FBX",
                "Persona",
                "PropertyEditor",
                "LandscapeEditor",
                "MaterialEditor"
            });
        }

		if ((Target.Platform == UnrealTargetPlatform.Win64) || (Target.Platform == UnrealTargetPlatform.Win32))
		{
			if (UseThirdPartyPython)
			{
				PythonHome = ThirdPartyPythonHome;

				System.Console.WriteLine("Using Embedded Python at: " + PythonHome);
				PublicIncludePaths.Add(PythonHome);
				string libPath = Path.Combine(PythonHome, "Lib", string.Format("{0}.lib", PythonType.ToLower()));

				System.Console.WriteLine("full lib path: " + libPath);
				PublicLibraryPaths.Add(Path.GetDirectoryName(libPath));
				PublicAdditionalLibraries.Add(libPath);

				string dllPath = Path.Combine(BinariesPath, "Win64", string.Format("{0}.dll", PythonType.ToLower()));
				RuntimeDependencies.Add(dllPath);

				AddRuntimeDependenciesForCopying(Target);
			}
			else if (PythonHome == "")
			{
				PythonHome = DiscoverPythonPath(windowsKnownPaths, "Win64");
				if (PythonHome == "")
				{
					throw new System.Exception("Unable to find Python installation");
				}

				System.Console.WriteLine("Using Python at: " + PythonHome);
				PublicIncludePaths.Add(PythonHome);
				string libPath = GetWindowsPythonLibFile(PythonHome);
				PublicLibraryPaths.Add(Path.GetDirectoryName(libPath));
				PublicAdditionalLibraries.Add(libPath);
			}
		}

		//other platforms
		else
		{
			if (PythonHome == "")
			{
				PythonHome = DiscoverPythonPath(macKnownPaths, "Mac");
				if (PythonHome == "")
				{
					throw new System.Exception("Unable to find Python installation");
				}
				System.Console.WriteLine("Using Python at: " + PythonHome);
				PublicIncludePaths.Add(PythonHome);
				PublicAdditionalLibraries.Add(Path.Combine(PythonHome, "Lib", string.Format("{0}.lib", PythonType)));
			}
			System.Console.WriteLine("Using Python at: " + PythonHome);
			PublicIncludePaths.Add(PythonHome);
			string libPath = GetMacPythonLibFile(PythonHome);
			PublicLibraryPaths.Add(Path.GetDirectoryName(libPath));
			PublicDelayLoadDLLs.Add(libPath);
		}
        if (Target.Platform == UnrealTargetPlatform.Linux)
        {
            if (PythonHome == "")
            {
                string includesPath = DiscoverLinuxPythonIncludesPath();
                if (includesPath == null)
                {
                    throw new System.Exception("Unable to find Python includes, please add a search path to linuxKnownIncludesPaths");
                }
                string libsPath = DiscoverLinuxPythonLibsPath();
                if (libsPath == null)
                {
                    throw new System.Exception("Unable to find Python libs, please add a search path to linuxKnownLibsPaths");
                }
                PublicIncludePaths.Add(includesPath);
                PublicAdditionalLibraries.Add(libsPath);

            }
            else
            {
                string[] items = PythonHome.Split(';');
                PublicIncludePaths.Add(items[0]);
                PublicAdditionalLibraries.Add(items[1]);
            }
        }
    }

    private bool IsPathRelative(string Path)
    {
        bool IsRooted = Path.StartsWith("\\", System.StringComparison.Ordinal) || // Root of the current directory on Windows. Also covers "\\" for UNC or "network" paths.
                        Path.StartsWith("/", System.StringComparison.Ordinal) ||  // Root of the current directory on Windows, root on UNIX-likes.
                                                                                  // Also covers "\\", considering normalization replaces "\\" with "//".
                        (Path.Length >= 2 && char.IsLetter(Path[0]) && Path[1] == ':'); // Starts with "<DriveLetter>:"
        return !IsRooted;
    }

    private string DiscoverPythonPath(string[] knownPaths, string binaryPath)
    {
        // insert the PYTHONHOME content as the first known path
        List<string> paths = new List<string>(knownPaths);
        paths.Insert(0, Path.Combine(ModuleDirectory, "../../Binaries", binaryPath));
        string environmentPath = System.Environment.GetEnvironmentVariable("PYTHONHOME");
        if (!string.IsNullOrEmpty(environmentPath))
            paths.Insert(0, environmentPath);

        foreach (string path in paths)
        {
            string actualPath = path;

            if (IsPathRelative(actualPath))
            {
                actualPath = Path.GetFullPath(Path.Combine(ModuleDirectory, actualPath));
            }

            string headerFile = Path.Combine(actualPath, "include", "Python.h");
            if (File.Exists(headerFile))
            {
                return actualPath;
            }
            // this is mainly useful for OSX
            headerFile = Path.Combine(actualPath, "Headers", "Python.h");
            if (File.Exists(headerFile))
            {
                return actualPath;
            }
        }
        return "";
    }

    private string DiscoverLinuxPythonIncludesPath()
    {
        List<string> paths = new List<string>(linuxKnownIncludesPaths);
        paths.Insert(0, Path.Combine(ModuleDirectory, "../../Binaries", "Linux", "include"));
        foreach (string path in paths)
        {
            string headerFile = Path.Combine(path, "Python.h");
            if (File.Exists(headerFile))
            {
                return path;
            }
        }
        return null;
    }

    private string DiscoverLinuxPythonLibsPath()
    {
        List<string> paths = new List<string>(linuxKnownLibsPaths);
        paths.Insert(0, Path.Combine(ModuleDirectory, "../../Binaries", "Linux", "lib"));
        paths.Insert(0, Path.Combine(ModuleDirectory, "../../Binaries", "Linux", "lib64"));
        foreach (string path in paths)
        {
            if (File.Exists(path))
            {
                return path;
            }
        }
        return null;
    }

    private string GetMacPythonLibFile(string basePath)
    {
        // first try with python3
        for (int i = 9; i >= 0; i--)
        {
            string fileName = string.Format("libpython3.{0}.dylib", i);
            string fullPath = Path.Combine(basePath, "lib", fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
            fileName = string.Format("libpython3.{0}m.dylib", i);
            fullPath = Path.Combine(basePath, "lib", fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        // then python2
        for (int i = 9; i >= 0; i--)
        {
            string fileName = string.Format("libpython2.{0}.dylib", i);
            string fullPath = Path.Combine(basePath, "lib", fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
            fileName = string.Format("libpython2.{0}m.dylib", i);
            fullPath = Path.Combine(basePath, "lib", fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new System.Exception("Invalid Python installation, missing .dylib files");
    }

    private string GetWindowsPythonLibFile(string basePath)
    {
        // just for usability, report if the PythonHome is not in the system path
        string[] allPaths = System.Environment.GetEnvironmentVariable("PATH").Split(';');
        // this will transform the slashes in backslashes...
        string checkedPath = Path.GetFullPath(basePath);
        if (checkedPath.EndsWith("\\"))
        {
            checkedPath = checkedPath.Remove(checkedPath.Length - 1);
        }
        bool found = false;
        foreach (string item in allPaths)
        {
            if (item == checkedPath || item == checkedPath + "\\")
            {
                found = true;
                break;
            }
        }
        if (!found)
        {
            System.Console.WriteLine("[WARNING] Your Python installation is not in the system PATH environment variable.");
            System.Console.WriteLine("[WARNING] Ensure your python paths are set in GlobalConfig (DefaultEngine.ini) so the path can be corrected at runtime.");
        }
        // first try with python3
        for (int i = 9; i >= 0; i--)
        {
            string fileName = string.Format("python3{0}.lib", i);
            string fullPath = Path.Combine(basePath, "libs", fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        // then python2
        for (int i = 9; i >= 0; i--)
        {
            string fileName = string.Format("python2{0}.lib", i);
            string fullPath = Path.Combine(basePath, "libs", fileName);
            if (File.Exists(fullPath))
            {
                return fullPath;
            }
        }

        throw new System.Exception("Invalid Python installation, missing .lib files");
    }
}
