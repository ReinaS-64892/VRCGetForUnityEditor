using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UIElements;
using System;

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

            var project = RequestVRCGet.GetPackages();

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

            content.hierarchy.Add(new Label("Packages---------"));

            var packageContainer = new VisualElement();
            content.hierarchy.Add(packageContainer);

            foreach (var package in project.packages)
            {
                var packageVi = new VisualElement();
                packageVi.AddToClassList("PackageContainer");

                packageVi.hierarchy.Add(new Label(package.name));

                var rightElement = new VisualElement();
                rightElement.AddToClassList("PackageContainerRight");
                packageVi.hierarchy.Add(rightElement);
                if (!TryCreateVersionSelector(package, rightElement)) { rightElement.hierarchy.Add(new Label(string.IsNullOrWhiteSpace(package.installed) ? "not installed" : package.installed)); }

                var removeButton = new Button(() => { Debug.Log(RequestVRCGet.Remove(package.name)); });
                removeButton.text = "Remove";

                rightElement.hierarchy.Add(removeButton);

                packageContainer.hierarchy.Add(packageVi);
            }

            static bool TryCreateVersionSelector(Package package, VisualElement packageVi)
            {
                if (string.IsNullOrWhiteSpace(package.locked) || string.IsNullOrWhiteSpace(package.installed)) { return false; }

                var versions = RequestVRCGet.GetVersions(package.name);
                if (versions == null) { return false; }

                var popup = new PopupField<string>(versions, package.installed);
                popup.RegisterValueChangedCallback(v => RequestVRCGet.Install(package.name, v.newValue));
                packageVi.hierarchy.Add(popup);
                return true;
            }
            var addPackagesContainer = new VisualElement();
            var addWindowButton = new Button(() => ShowAddPackages(addPackagesContainer));
            addWindowButton.text = "ShowAddPackages";
            content.hierarchy.Add(addWindowButton);
            content.hierarchy.Add(addPackagesContainer);

            static void ShowAddPackages(VisualElement root)
            {
                root.hierarchy.Clear();
                foreach (var url in RequestVRCGet.Repositories())
                {
                    var container = new Foldout();
                    container.text = url;
                    container.value = false;
                    var foldingContainer = container.Q("unity-content");
                    try
                    {
                        var names = RequestVRCGet.PackageNames(url);
                        if (names == null) { continue; }
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
                        root.hierarchy.Add(container);
                    }
                    catch (Exception e)
                    {
                        Debug.LogError(e);
                        Debug.LogError(url);
                    }
                }

            }





        }

    }
}