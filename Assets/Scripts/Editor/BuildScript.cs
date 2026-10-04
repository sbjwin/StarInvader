using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace StarInvader.Editor
{
    public static class BuildScript
    {
        private static readonly string[] Scenes = new string[]
        {
            "Assets/Scenes/TitleScene.unity",
            "Assets/Scenes/GameScene.unity",
            "Assets/Scenes/GameOverScene.unity"
        };

        [MenuItem("Star Invader/🚀 PC (Windows) 실행 파일 원클릭 빌드", false, 10)]
        public static void BuildWindowsMenu()
        {
            PerformWindowsBuild(openFolderOnSuccess: true);
        }

        public static void BuildWindows()
        {
            PerformWindowsBuild(openFolderOnSuccess: false);
        }

        private static void PerformWindowsBuild(bool openFolderOnSuccess)
        {
            string buildDir = Path.Combine(Directory.GetCurrentDirectory(), "Builds", "Windows");
            if (!Directory.Exists(buildDir))
            {
                Directory.CreateDirectory(buildDir);
            }

            string exePath = Path.Combine(buildDir, "StarInvader.exe");
            Debug.Log($"[StarInvader] Windows 64-bit 빌드 시작: {exePath}");

            BuildPlayerOptions buildPlayerOptions = new BuildPlayerOptions
            {
                scenes = Scenes,
                locationPathName = exePath,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None
            };

            BuildReport report = BuildPipeline.BuildPlayer(buildPlayerOptions);
            BuildSummary summary = report.summary;

            if (summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"[StarInvader] 빌드 대성공! 총 크기: {summary.totalSize / (1024 * 1024):F2} MB, 소요 시간: {summary.totalTime.TotalSeconds:F1}초");
                if (openFolderOnSuccess)
                {
                    EditorUtility.RevealInFinder(exePath);
                }
            }
            else if (summary.result == BuildResult.Failed)
            {
                Debug.LogError($"[StarInvader] 빌드 실패! 오류 개수: {summary.totalErrors}");
            }
            else
            {
                Debug.LogWarning($"[StarInvader] 빌드 결과: {summary.result}");
            }
        }
    }
}
