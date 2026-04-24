#if UNITY_EDITOR
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace CastleWar.EditorTools
{
    public static class BuildMenu
    {
        private const string BuildRootFolder = "Builds";
        private const string ProductName = "CastleWar";

        // 플랫폼별 서브 폴더/실행 파일 이름
        private const string WindowsFolder = "Windows";
        private const string WindowsExeName = ProductName + ".exe";

        // 기본 해상도 (창 모드)
        private const int WindowsWidth = 840;
        private const int WindowsHeight = 640;

        // 안드로이드는 추후 지원 예정 — 경로/해상도만 남겨둠
        // private const string AndroidFolder = "Android";
        // private const string AndroidApkName = ProductName + ".apk";
        // private const int AndroidWidth = 840;
        // private const int AndroidHeight = 640;

        [MenuItem("Tools/Builds/Build Client", priority = 0)]
        public static void BuildClient()
        {
            BuildWindows();

            // 안드로이드 빌드 스위칭 예시 — 필요 시 주석 해제
            // BuildAndroid();
        }

        [MenuItem("Tools/Builds/Run Player 1", priority = 20)]
        public static void RunPlayer1() => RunWindowsInstances(1);

        [MenuItem("Tools/Builds/Run Player 2", priority = 21)]
        public static void RunPlayer2() => RunWindowsInstances(2);

        [MenuItem("Tools/Builds/Run Player 3", priority = 22)]
        public static void RunPlayer3() => RunWindowsInstances(3);

        [MenuItem("Tools/Builds/Run Player 4", priority = 23)]
        public static void RunPlayer4() => RunWindowsInstances(4);

        private static void BuildWindows()
        {
            string outputDir = Path.Combine(GetProjectRoot(), BuildRootFolder, WindowsFolder);
            Directory.CreateDirectory(outputDir);
            string outputPath = Path.Combine(outputDir, WindowsExeName);

            ApplyStandaloneWindowSettings();

            var options = new BuildPlayerOptions
            {
                scenes = GetEnabledScenes(),
                locationPathName = outputPath,
                target = BuildTarget.StandaloneWindows64,
                targetGroup = BuildTargetGroup.Standalone,
                options = BuildOptions.None,
            };

            Debug.Log($"[BuildMenu] Windows 빌드 시작 → {outputPath}");
            var report = BuildPipeline.BuildPlayer(options);
            var summary = report.summary;

            if (summary.result == UnityEditor.Build.Reporting.BuildResult.Succeeded)
                Debug.Log($"[BuildMenu] 빌드 성공 ({summary.totalSize / 1024 / 1024} MB, {summary.totalTime.TotalSeconds:F1}s)");
            else
                Debug.LogError($"[BuildMenu] 빌드 실패: {summary.result}");
        }

        private static void ApplyStandaloneWindowSettings()
        {
            PlayerSettings.defaultScreenWidth = WindowsWidth;
            PlayerSettings.defaultScreenHeight = WindowsHeight;
            PlayerSettings.fullScreenMode = FullScreenMode.Windowed;
            PlayerSettings.defaultIsNativeResolution = false;
            PlayerSettings.resizableWindow = false;
            PlayerSettings.displayResolutionDialog = ResolutionDialogSetting.Disabled;
        }

        // 안드로이드 기본 창 크기 설정 예시 — 필요 시 주석 해제
        // private static void ApplyAndroidWindowSettings()
        // {
        //     PlayerSettings.Android.defaultWindowWidth = AndroidWidth;
        //     PlayerSettings.Android.defaultWindowHeight = AndroidHeight;
        //     PlayerSettings.Android.startInFullscreen = false;
        // }

        // private static void BuildAndroid()
        // {
        //     string outputDir = Path.Combine(GetProjectRoot(), BuildRootFolder, AndroidFolder);
        //     Directory.CreateDirectory(outputDir);
        //     string outputPath = Path.Combine(outputDir, AndroidApkName);
        //
        //     var options = new BuildPlayerOptions
        //     {
        //         scenes = GetEnabledScenes(),
        //         locationPathName = outputPath,
        //         target = BuildTarget.Android,
        //         targetGroup = BuildTargetGroup.Android,
        //         options = BuildOptions.None,
        //     };
        //
        //     Debug.Log($"[BuildMenu] Android 빌드 시작 → {outputPath}");
        //     BuildPipeline.BuildPlayer(options);
        // }

        private static void RunWindowsInstances(int count)
        {
            string exePath = Path.Combine(GetProjectRoot(), BuildRootFolder, WindowsFolder, WindowsExeName);

            if (!File.Exists(exePath))
            {
                Debug.LogError($"[BuildMenu] 빌드된 exe를 찾을 수 없습니다: {exePath}\nTools/Builds/Build Client 먼저 실행하세요.");
                return;
            }

            for (int i = 0; i < count; i++)
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = Path.GetDirectoryName(exePath),
                        UseShellExecute = true,
                    };
                    Process.Start(psi);
                    Debug.Log($"[BuildMenu] Player {i + 1} 실행");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[BuildMenu] Player {i + 1} 실행 실패: {e.Message}");
                }
            }
        }

        // 안드로이드 실행은 adb install + am start 흐름 — 추후 작성
        // private static void RunAndroidInstance() { }

        private static string[] GetEnabledScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            var paths = new System.Collections.Generic.List<string>();
            foreach (var s in scenes)
            {
                if (s.enabled) paths.Add(s.path);
            }

            if (paths.Count == 0)
            {
                // Build Settings가 비어있으면 현재 열린 씬 사용
                var active = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
                if (!string.IsNullOrEmpty(active.path))
                    paths.Add(active.path);
                else
                    Debug.LogWarning("[BuildMenu] Build Settings에 등록된 씬이 없고 활성 씬도 저장되지 않았습니다.");
            }
            return paths.ToArray();
        }

        private static string GetProjectRoot()
        {
            // Application.dataPath = <project>/Assets
            return Directory.GetParent(Application.dataPath).FullName;
        }
    }
}
#endif
