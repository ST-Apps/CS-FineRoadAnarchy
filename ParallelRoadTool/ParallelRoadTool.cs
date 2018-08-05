﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;
using ColossalFramework;
using ColossalFramework.Globalization;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using ICities;
using ParallelRoadTool.Detours;
using ParallelRoadTool.UI;
using ParallelRoadTool.Models;
using ParallelRoadTool.UI.Base;
using ParallelRoadTool.Utils;
using UnityEngine;

namespace ParallelRoadTool
{
    /// <summary>
    ///     Mod's main controller and data storage.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ParallelRoadTool : MonoBehaviour
    {
        #region Properties

        #region Data

        public List<NetInfo> AvailableRoadTypes { get; private set; }
        public List<NetTypeItem> SelectedRoadTypes { get; private set; }

        public string[] AvailableRoadNames;

        public static bool IsInGameMode;
        public bool IsSnappingEnabled;
        public bool IsLeftHandTraffic;

        private bool _isToolActive;
        public bool IsToolActive
        {
            get => _isToolActive && ToolsModifierControl.GetTool<NetTool>().enabled;

            private set
            {
                if (IsToolActive == value) return;                
                ToggleDetours(value);
                _isToolActive = value;                
            }
        }        

        #endregion

        #region UI

        private UIMainWindow _mainWindow;

        #endregion

        #endregion        

        #region Unity

        public void Start()
        {            
            try
            {
                // Find NetTool and deploy
                if (ToolsModifierControl.GetTool<NetTool>() == null)
                {
                    DebugUtils.Log("Net Tool not found");
                    enabled = false;
                    return;
                }

                DebugUtils.Log("Loading PRT...");

                // Init support data
                var count = PrefabCollection<NetInfo>.PrefabCount();
                AvailableRoadNames = new string[count + 1];
                AvailableRoadTypes = new List<NetInfo>();
                SelectedRoadTypes = new List<NetTypeItem>();
                IsSnappingEnabled = false;
                IsLeftHandTraffic = Singleton<SimulationManager>.instance.m_metaData.m_invertTraffic ==
                                    SimulationMetaData.MetaBool.True;
                IsToolActive = false;                

                // Available networks loading
                DebugUtils.Log("Loading all available networks...");
                // Default item, creates a net with the same type as source
                AddNetworkType(null);
                for (uint i = 0; i < count; i++)
                {
                    var prefab = PrefabCollection<NetInfo>.GetPrefab(i);                    
                    if (prefab != null) AddNetworkType(prefab);
                }
                DebugUtils.Log($"Loaded {AvailableRoadTypes.Count} networks.");

                // Main UI init
                DebugUtils.Log("Adding UI components");
                var view = UIView.GetAView();
                _mainWindow = view.FindUIComponent<UIMainWindow>("PRT_MainWindow");
                if (_mainWindow != null)
                    Destroy(_mainWindow);                
                _mainWindow = view.AddUIComponent(typeof(UIMainWindow)) as UIMainWindow;

                SubscribeToUIEvents();
                DebugUtils.Log("Initialized");
            }
            catch (Exception e)
            {
                DebugUtils.Log("Start failed");
                DebugUtils.LogException(e);
                enabled = false;
            }
        }

        public void OnDestroy()
        {
            try
            {
                DebugUtils.Log("Destroying ...");

                ToggleDetours(false);
                UnsubscribeToUIEvents();

                // Reset data structures
                AvailableRoadTypes.Clear();
                SelectedRoadTypes.Clear();
                IsToolActive = false;
                IsSnappingEnabled = false;
                IsLeftHandTraffic = false;
                Destroy(_mainWindow);
                _mainWindow = null;
            }
            catch
            {
                // HACK - [ISSUE 31]
            }
        }

        #endregion

        #region Utils

        private static void ToggleDetours(bool toolEnabled)
        {
            if (toolEnabled)
            {
                DebugUtils.Log("Enabling parallel road support");
                NetManagerDetour.Deploy();
                NetToolDetour.Deploy();
            }
            else
            {
                DebugUtils.Log("Disabling parallel road support");
                NetManagerDetour.Revert();
                NetToolDetour.Revert();
            }
        }

        private void AddNetworkType(NetInfo net)
        {
            AvailableRoadNames[AvailableRoadTypes.Count] = net.GenerateBeautifiedNetName();
            AvailableRoadTypes.Add(net);          
        }

        #endregion

        #region Handlers

        private void UnsubscribeToUIEvents()
        {
            _mainWindow.OnParallelToolToggled -= MainWindowOnOnParallelToolToggled;
            _mainWindow.OnNetworksListCountChanged -= MainWindowOnOnNetworksListCountChanged;
            _mainWindow.OnSnappingToggled -= MainWindowOnOnSnappingToggled;
            _mainWindow.OnHorizontalOffsetKeypress -= MainWindowOnOnHorizontalOffsetKeypress;
            _mainWindow.OnVerticalOffsetKeypress -= MainWindowOnOnVerticalOffsetKeypress;
        }

        private void SubscribeToUIEvents()
        {
            _mainWindow.OnParallelToolToggled += MainWindowOnOnParallelToolToggled;
            _mainWindow.OnNetworksListCountChanged += MainWindowOnOnNetworksListCountChanged;
            _mainWindow.OnSnappingToggled += MainWindowOnOnSnappingToggled;
            _mainWindow.OnHorizontalOffsetKeypress += MainWindowOnOnHorizontalOffsetKeypress;
            _mainWindow.OnVerticalOffsetKeypress += MainWindowOnOnVerticalOffsetKeypress;
        }

        private void MainWindowOnOnVerticalOffsetKeypress(UIComponent component, float step)
        {
            for (var i = 0; i < SelectedRoadTypes.Count; i++)
            {
                SelectedRoadTypes[i].VerticalOffset += (1 + i) * step;
            }
        }

        private void MainWindowOnOnHorizontalOffsetKeypress(UIComponent component, float step)
        {
            for (var i = 0; i < SelectedRoadTypes.Count; i++)
            {
                SelectedRoadTypes[i].HorizontalOffset += (1 + i) * step;
            }
        }

        private void MainWindowOnOnSnappingToggled(UIComponent component, bool value)
        {
            IsSnappingEnabled = value;
        }

        private void MainWindowOnOnNetworksListCountChanged(object sender, System.EventArgs e)
        {
            NetManagerDetour.NetworksCount = SelectedRoadTypes.Count;
        }

        private void MainWindowOnOnParallelToolToggled(UIComponent component, bool value)
        {
            IsToolActive = value;

            if (value && ToolsModifierControl.advisorPanel.isVisible && ToolsModifierControl.advisorPanel.isOpen)
                _mainWindow.ShowTutorial();
        }

        #endregion        
    }
}
