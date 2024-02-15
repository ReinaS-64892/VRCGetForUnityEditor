using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.Linq;

namespace net.rs64.VRCGetForUnityEditor
{

    internal class VRCGetForUnityEditor : EditorWindow
    {
        [SerializeField]
        private StyleSheet uss;

        [MenuItem("Tools/VRCGetForUnityEditor")]
        private static void ShowWindow()
        {
            var window = GetWindow<VRCGetForUnityEditor>();
            window.titleContent = new GUIContent("VRCGetForUnityEditor");
            window.Show();
        }

        public void CreateGUI()
        {
            rootVisualElement.Clear();

            uss ??= AssetDatabase.LoadAssetAtPath<StyleSheet>(AssetDatabase.GUIDToAssetPath("5ca71c7c917a6d2449e4157f103d7549"));
            if (uss != null) { rootVisualElement.styleSheets.Add(uss); }
            else { Debug.LogWarning("スタイルが存在しません"); }

            var syncContext = SynchronizationContext.Current;

            Task.Run(RequestVRCGet.GetPackages).ContinueWith(async (projectTask, _) =>
            {
                var project = await projectTask;
                project.packages = project.packages.OrderBy(i => i.name).ToArray();

                syncContext.Post(_ => //ポスト地獄...何とかならんの？？？
                {

                    var reDrawButton = new Button(CreateGUI);
                    reDrawButton.text = "Refresh";
                    rootVisualElement.Add(reDrawButton);

                    rootVisualElement.Add(new Label($"ThisProjectVersion : {project.unity_version}"));

                    var scrollView = new ScrollView();
                    var content = scrollView.Q<VisualElement>("unity-content-container");

                    rootVisualElement.Add(scrollView);

                    var upgradeButton = new Button(RequestVRCGet.Upgrade);
                    upgradeButton.text = "Upgrade";
                    content.hierarchy.Add(upgradeButton);

                    var containsPackagesHeader = new Label("ContainsPackages---------");
                    containsPackagesHeader.AddToClassList("Header");
                    content.hierarchy.Add(containsPackagesHeader);

                    var packageContainer = new VisualElement();
                    content.hierarchy.Add(packageContainer);

                    foreach (var package in project.packages)
                    {
                        var packageVi = new VisualElement();
                        packageVi.AddToClassList("PackageContainer");

                        CreatePackageRow(package, packageVi);

                        packageContainer.hierarchy.Add(packageVi);
                    }

                    var addPackagesHeader = new Label("AddPackage---------");
                    addPackagesHeader.AddToClassList("Header");
                    content.hierarchy.Add(addPackagesHeader);
                    var addPackagesContainer = new VisualElement();
                    content.hierarchy.Add(addPackagesContainer);

                    ShowAddPackages(addPackagesContainer);

                }, null);

            }, null);


            async void ShowAddPackages(VisualElement root)
            {
                root.hierarchy.Clear();
                var repositories = await Task.Run(RequestVRCGet.Repositories);
                repositories.Sort();
                syncContext.Post(_ =>
                    {
                        foreach (var url in repositories)
                        {
                            var container = new Foldout();
                            container.text = url;
                            container.value = false;
                            var foldingContainer = container.Q("unity-content");
                            try
                            {
                                CreatePackages(url, foldingContainer);
                                root.hierarchy.Add(container);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                                Debug.LogError(url);
                            }
                        }
                    }, null);

                async void CreatePackages(string url, VisualElement foldingContainer)
                {
                    var names = await Task.Run(() => RequestVRCGet.PackageNames(url));
                    names.Sort();
                    syncContext.Post(_ =>
                    {
                        if (names == null) { return; }
                        foreach (var packageName in names)
                        {
                            var packageContainer = new VisualElement();
                            packageContainer.AddToClassList("PackageContainer");

                            packageContainer.hierarchy.Add(new Label(packageName));
                            var addButton = new Button(() => RequestVRCGet.Install(packageName));
                            addButton.text = "Add";
                            packageContainer.hierarchy.Add(addButton);

                            foldingContainer.hierarchy.Add(packageContainer);
                        }
                    }, null);
                }
            }

            async void CreatePackageRow(Package package, VisualElement packageVi)
            {
                packageVi.hierarchy.Add(new Label(package.name));

                var rightElement = new VisualElement();
                rightElement.AddToClassList("PackageContainerRight");
                packageVi.hierarchy.Add(rightElement);

                var versions = await Task.Run(() => { var versions = RequestVRCGet.GetVersions(package.name); versions.Sort(); versions.Reverse(); return versions; });
                syncContext.Post(_ =>
                {

                    if (string.IsNullOrWhiteSpace(package.locked) || string.IsNullOrWhiteSpace(package.installed) || versions == null)
                    {
                        rightElement.hierarchy.Add(new Label(string.IsNullOrWhiteSpace(package.installed) ? "not installed" : package.installed));
                    }
                    else
                    {
                        var popup = new PopupField<string>(versions, package.installed);
                        popup.RegisterValueChangedCallback(v => RequestVRCGet.Install(package.name, v.newValue));
                        rightElement.hierarchy.Add(popup);
                    }

                    var removeButton = new Button(() => { Debug.Log(RequestVRCGet.Remove(package.name)); });
                    removeButton.text = "Remove";

                    rightElement.hierarchy.Add(removeButton);

                }, null);

            }

        }

    }
}