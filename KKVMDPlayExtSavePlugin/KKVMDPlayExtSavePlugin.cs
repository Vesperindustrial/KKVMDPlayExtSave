using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using BepInEx;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using KKVMDPlayPlugin;
using Studio;
using UnityEngine;

namespace KKVMDPlayExtSavePlugin
{
	// Token: 0x02000002 RID: 2
	[BepInDependency("com.bepis.bepinex.extendedsave", BepInDependency.DependencyFlags.HardDependency)]
	[BepInPlugin("KKVMDPlayExtSavePlugin.KKVMDPlayExtSavePlugin", "KKVMDPlayExtSavePlugin", "0.0.12")]
	public class KKVMDPlayExtSavePlugin : BaseUnityPlugin
	{
        public const string NAME = "KKVMDPlayExtSavePlugin";
        public const string VERSION = "0.0.11";
        public const string EXT_SAVE_ID = "KKVMDPlayExtSave";
        private bool initialized;
        private static bool needReload = false;
        private static KKVMDAnimationDataSaveLoad saveLoadForChara = new KKVMDAnimationDataSaveLoad();
        private static KKVMDAnimationSceneDataSaveLoadHandler saveLoadForScene = new KKVMDAnimationSceneDataSaveLoadHandler();
        private const string VMD_CHARA_TAG = "VMDCharaData";
        private const string VMD_SCENE_TAG = "VMDSceneData";
        internal static new ManualLogSource Logger;

        // Token: 0x06000001 RID: 1 RVA: 0x00002050 File Offset: 0x00000250
        private void OnLevelWasLoaded(int level)
		{
			if (!this.initialized)
			{
				//string entry = Config.GetEntry("SaveInScene", "True", "KKVMDPlayExtSave");
				//Config.SetEntry("SaveInScene", entry, "KKVMDPlayExtSave");
				//Config.SaveConfig();
				bool flag;
				//if (bool.TryParse(entry, out flag) && flag)
				//{
					KKVMDPlayExtSavePlugin.InstallHooks();
				//}

				this.initialized = true;
			}
		}

		// Token: 0x06000002 RID: 2 RVA: 0x000020A8 File Offset: 0x000002A8
		private void LateUpdate()
		{
			if (KKVMDPlayExtSavePlugin.needReload)
			{
				KKVMDPlayExtSavePlugin.needReload = false;
				base.StartCoroutine(this.LoadDataCo());
			}
		}

		// Token: 0x06000003 RID: 3 RVA: 0x000020C4 File Offset: 0x000002C4
		private IEnumerator LoadDataCo()
		{
			yield return new WaitForEndOfFrame();
			foreach (ObjectCtrlInfo objectCtrlInfo in Singleton<Studio.Studio>.Instance.dicObjectCtrl.Values)
			{
				if (objectCtrlInfo is OCIChar)
				{
					VMDAnimationController.Install((objectCtrlInfo as OCIChar).charInfo);
				}
			}
			yield return new WaitForSeconds(1f);
			KKVMDPlayExtSavePlugin.ExtendedSceneLoadInUpdate();
			yield break;
		}

		// Token: 0x06000004 RID: 4 RVA: 0x000020CC File Offset: 0x000002CC
		public static void InstallHooks()
		{
			ExtendedSave.SceneBeingLoaded += new ExtendedSave.SceneEventHandler(KKVMDPlayExtSavePlugin.ExtendedSceneLoad);
			ExtendedSave.SceneBeingSaved += new ExtendedSave.SceneEventHandler(KKVMDPlayExtSavePlugin.ExtendedSceneSave);
		}

		// Token: 0x06000005 RID: 5 RVA: 0x000020F0 File Offset: 0x000002F0
		private static void ExtendedSceneLoad(string path)
		{
			KKVMDPlayExtSavePlugin.needReload = true;
		}

		// Token: 0x06000006 RID: 6 RVA: 0x000020F8 File Offset: 0x000002F8
		private static void ExtendedSceneLoadInUpdate()
		{
			try
			{
				Logger.Log(LogLevel.Info, "Start loading VMDPlay info from scene data.");
				object obj;
				if (ExtendedSave.GetSceneExtendedDataById("KKVMDPlayExtSave").data.TryGetValue("xml", out obj))
				{
					if (obj != null && obj is byte[])
					{
						Logger.Log(LogLevel.Info, string.Format("Found VMDPlay info XML data: {0}", ((byte[])obj).Length));
						MemoryStream inStream = new MemoryStream((byte[])obj);
						Console.WriteLine("ExtSave: Loading from PNG.");
						XmlDocument xmlDocument = new XmlDocument();
						xmlDocument.Load(inStream);
						Console.WriteLine(xmlDocument.ToString());
						KKVMDPlayExtSavePlugin.OnLoad(xmlDocument.DocumentElement);
					}
					else
					{
						Logger.Log(LogLevel.Message, "Data not found.");
					}
				}
				else
				{
					Logger.Log(LogLevel.Message, "Data not found.");
				}
			}
			catch (Exception ex)
			{
				Logger.Log(LogLevel.Error, string.Format("Failed to load data. {0}", ex.StackTrace));
			}
		}

		// Token: 0x06000007 RID: 7 RVA: 0x000021D8 File Offset: 0x000003D8
		private static void ExtendedSceneSave(string path)
		{
			try
			{
				Logger.Log(LogLevel.Info, string.Format("Start saving VMDPlay info into scene data: {0}", path));
				PluginData pluginData = new PluginData();
				XmlDocument xmlDocument = new XmlDocument();
				XmlDeclaration newChild = xmlDocument.CreateXmlDeclaration("1.0", "utf-8", null);
				xmlDocument.AppendChild(newChild);
				XmlElement xmlElement = xmlDocument.CreateElement("VMDPlaySaveData");
				xmlDocument.AppendChild(xmlElement);
				KKVMDPlayExtSavePlugin.OnSave(xmlElement);
				pluginData.data["xml"] = KKVMDPlayExtSavePlugin.GetExtDataAsBytes(xmlDocument);
				ExtendedSave.SetSceneExtendedDataById("KKVMDPlayExtSave", pluginData);
				Logger.Log(LogLevel.Info, string.Format("Save completed: {0}", path));
			}
			catch (Exception ex)
			{
				Logger.Log(LogLevel.Error, string.Format("Failed to load data. {0}", ex.StackTrace));
			}
		}

		// Token: 0x06000008 RID: 8 RVA: 0x00002298 File Offset: 0x00000498
		private static byte[] GetExtDataAsBytes(XmlDocument doc)
		{
			MemoryStream memoryStream = new MemoryStream();
			doc.Save(memoryStream);
			memoryStream.Close();
			return memoryStream.ToArray();
		}

		// Token: 0x06000009 RID: 9 RVA: 0x000022C0 File Offset: 0x000004C0
		public static void OnLoad(XmlElement rootElement)
		{
			Studio.Studio instance = Singleton<Studio.Studio>.Instance;
			List<OCIChar> list = new List<OCIChar>();
			foreach (KeyValuePair<int, ObjectCtrlInfo> keyValuePair in from e in instance.dicObjectCtrl
			orderby e.Key
			select e)
			{
				if (keyValuePair.Value is OCIChar)
				{
					list.Add(keyValuePair.Value as OCIChar);
				}
			}
			foreach (object obj in rootElement.ChildNodes)
			{
				XmlElement xmlElement = (XmlElement)obj;
				if (xmlElement.Name == "VMDCharaData")
				{
					int num = int.Parse(xmlElement.GetAttribute("dicKey"));
					ObjectCtrlInfo objectCtrlInfo = null;
					instance.dicObjectCtrl.TryGetValue(num, out objectCtrlInfo);
					if (objectCtrlInfo == null)
					{
						Logger.Log(LogLevel.Error, string.Format("Character not found. dicKey: {0}", num));
					}
					else if (objectCtrlInfo is OCIChar)
					{
						KKVMDPlayExtSavePlugin.saveLoadForChara.LoadVMDAnimInfo(xmlElement, objectCtrlInfo);
					}
				}
				else if (xmlElement.Name == "VMDSceneData")
				{
					KKVMDPlayExtSavePlugin.saveLoadForScene.LoadVMDSceneDataInfo(xmlElement);
				}
				else
				{
					Logger.Log(LogLevel.Error, string.Format("Unknown Tag found. Load data {0}", KKVMDPlayExtSavePlugin.ToString(xmlElement)));
				}
			}
		}

		// Token: 0x0600000A RID: 10 RVA: 0x0000244C File Offset: 0x0000064C
		public static void OnSave(XmlElement rootElement)
		{
			try
			{
				rootElement.SetAttribute("Version", "1");
				KKVMDPlayExtSavePlugin.saveLoadForScene.OnSave(rootElement);
				Studio.Studio instance = Singleton<Studio.Studio>.Instance;
				new List<OCIChar>();
				foreach (KeyValuePair<int, ObjectCtrlInfo> keyValuePair in from e in instance.dicObjectCtrl
				orderby e.Key
				select e)
				{
					if (keyValuePair.Value is OCIChar)
					{
						OCIChar ocichar = keyValuePair.Value as OCIChar;
						XmlElement xmlElement = KKVMDPlayExtSavePlugin.AddVMDCharaInfoElem(rootElement, ocichar, ocichar.charInfo);
						if (xmlElement != null)
						{
							xmlElement.SetAttribute("dicKey", keyValuePair.Key.ToString());
						}
					}
				}
			}
			catch (Exception value)
			{
				Console.WriteLine(value);
			}
		}

		// Token: 0x0600000B RID: 11 RVA: 0x00002538 File Offset: 0x00000738
		private static XmlElement AddVMDCharaInfoElem(XmlElement root, OCIChar studioChara, ChaControl chara)
		{
			VMDAnimationController vmdanimationController = VMDAnimationController.Install(studioChara.charInfo);
			if (vmdanimationController != null)
			{
				XmlElement xmlElement = root.OwnerDocument.CreateElement("VMDCharaData");
				KKVMDPlayExtSavePlugin.saveLoadForChara.SaveVMDAnimInfo(xmlElement, vmdanimationController);
				root.AppendChild(xmlElement);
				return xmlElement;
			}
			return null;
		}

		// Token: 0x0600000C RID: 12 RVA: 0x00002584 File Offset: 0x00000784
		private static string ToString(XmlElement xmlElem)
		{
			StringWriter stringWriter = new StringWriter();
			XmlWriter w = new XmlTextWriter(stringWriter);
			xmlElem.WriteTo(w);
			return stringWriter.ToString();
		}

    }
}
