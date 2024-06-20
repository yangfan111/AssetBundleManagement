using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Common;
using UnityEngine;
using Utils.AssetManager;

namespace AssetBundleManagement
{
    public interface IRecorderSaver
    {
        void AddQueue(AssetInfo info, bool isAync, LoadPriority priority, RecordAssetType assetType);

        void LoadAssetStart(AssetInfo info);
        void LoadAssetEnd(AssetInfo info, bool isSucc);

        void LoadBundleStart(string bundle);
        void LoadBundleEnd(string bundle, bool isSucc);


        void UnloadAsset(AssetInfo info);
        void UnloadBundle(string bundle);

        void Save(string key);
        void Clear();

        void AddAssetResHandle(AssetResourceHandle handle);
        void AddAssetBundleHandle(AssetBundleResourceHandle handle);
    }

    public class RecorderSaver : IRecorderSaver
    {
        public enum CellState
        {
            None = 0,
            AddQueue,
            Loading,
            Succ,
            Fail, 
        }

        public class LoadAssetCell
        {
            public RecordAssetType RecAssetType;
            public DateTime Date;
            public string BundleName;
            public string AssetName;
            public Type AssetType;
            public LoadPriority PriorityType;
            public int QueueAddStartFrame;
            public float QueueAddStartTime;
            public int LoadStartFrame;
            public int LoadEndFrame;
            public float LoadStartTime;
            public float LoadEndTime;
            public CellState State;
            public bool IsAsync;
        }

        public class LoadBundleCell
        {
            public DateTime Date;
            public string BundleName;
            public int StartFrame;
            public int EndFrame;
            public float StartTime;
            public float EndTime;
            public CellState State;
        }

        public class UnloadCell
        {
            public DateTime Date;
            public int StartFrame;
            public float StartTime;
            public string BundleName;
            public string AssetName;
        }

        public class RefAssetCell
        {
            public string AssetName;
            public string BundleName;
            public int RefCount;
            public AssetLoadState LoadState;
            public bool CannotDestroy;
        }

        public class RefBundleCell
        {
            public string BundleName;
            public int RefCount;
            public AssetBundleLoadState LoadState;
            public bool CannotDestroy;
        }

        public Dictionary<AssetInfo, LoadAssetCell> AssetCells = new Dictionary<AssetInfo, LoadAssetCell>();
        public Dictionary<string, LoadBundleCell> BundleCells = new Dictionary<string, LoadBundleCell>();

        public Dictionary<AssetInfo, UnloadCell> UnloadAssetCells = new Dictionary<AssetInfo, UnloadCell>();
        public Dictionary<string, UnloadCell> UnloadBundleCells = new Dictionary<string, UnloadCell>();

        public List<RefAssetCell> RefAssetCells = new List<RefAssetCell>();
        public List<RefBundleCell> RefBundleCells = new List<RefBundleCell>();

        private List<LoadAssetCell> GetAssetCellClone()
        {
            var clone = new List<LoadAssetCell>();
            foreach (var pair in AssetCells)
            {
                clone.Add(pair.Value);
            }
            return clone;
        }

        private List<UnloadCell> GetUnloadAssetCellClone()
        {
            var clone = new List<UnloadCell>();
            foreach (var pair in UnloadAssetCells)
            {
                clone.Add(pair.Value);
            }
            return clone;
        }

        private List<LoadBundleCell> GetBundleCellClone()
        {
            var clone = new List<LoadBundleCell>();
            foreach (var pair in BundleCells)
            {
                clone.Add(pair.Value);
            }
            return clone;
        }

        private List<UnloadCell> GetUnloadBundleCellClone()
        {
            var clone = new List<UnloadCell>();
            foreach (var pair in UnloadBundleCells)
            {
                clone.Add(pair.Value);
            }
            return clone;
        }

        public void AddAssetResHandle(AssetResourceHandle handle)
        {
            RefAssetCell cell = new RefAssetCell();
            RefAssetCells.Add(cell);

            cell.AssetName = handle.AssetKey.AssetName;
            cell.BundleName = handle.AssetKey.BundleName;
            cell.RefCount = handle.ReferenceCount;
            cell.LoadState = handle.LoadState;
            cell.CannotDestroy = false;
        }

        public void AddAssetBundleHandle(AssetBundleResourceHandle handle)
        {
            RefBundleCell cell = new RefBundleCell();
            RefBundleCells.Add(cell);

            cell.BundleName = handle.BundleKey.BundleName;
            cell.RefCount = handle.GetReferenceCount();
            cell.LoadState = handle.LoadState;
            cell.CannotDestroy = handle.CannotDestroy;
        }

        public void AddQueue(AssetInfo info, bool isAync, LoadPriority priority, RecordAssetType assetType)
        {
            LoadAssetCell cell = null;
            if (!AssetCells.TryGetValue(info, out cell))
            {
                cell = new LoadAssetCell();
                AssetCells[info] = cell;
                cell.Date = DateTime.Now;
                cell.AssetName = info.AssetName;
                cell.BundleName = info.BundleName;
                cell.AssetType = info.AssetType;
                cell.QueueAddStartFrame = Time.frameCount;
                cell.QueueAddStartTime = Time.realtimeSinceStartup;
                cell.PriorityType = priority;
                cell.IsAsync = isAync;
                cell.RecAssetType = assetType;
                cell.State = CellState.AddQueue;
            }
        }

        public void LoadAssetStart(AssetInfo info)
        {
            LoadAssetCell cell = null;
            if (AssetCells.TryGetValue(info, out cell))
            {
                cell.LoadStartFrame = Time.frameCount;
                cell.LoadStartTime = Time.realtimeSinceStartup;
                cell.State = CellState.Loading;
            }
        }

        public void LoadAssetEnd(AssetInfo info, bool isSucc)
        {
            LoadAssetCell cell = null;
            if (AssetCells.TryGetValue(info, out cell))
            {
                cell.LoadEndFrame = Time.frameCount;
                cell.LoadEndTime = Time.realtimeSinceStartup;
                cell.State = isSucc ? CellState.Succ : CellState.Fail;
            }
        }

        public void LoadBundleStart(string bundle)
        {
            LoadBundleCell cell = null;
            if (!BundleCells.TryGetValue(bundle, out cell))
            {
                cell = new LoadBundleCell();
                BundleCells[bundle] = cell;
                cell.Date = DateTime.Now;
                cell.BundleName = bundle;
                cell.StartFrame = Time.frameCount;
                cell.StartTime = Time.realtimeSinceStartup;
                cell.State = CellState.Loading;
            }
        }

        public void LoadBundleEnd(string bundle, bool isSucc)
        {
            LoadBundleCell cell = null;
            if (BundleCells.TryGetValue(bundle, out cell))
            {
                cell.EndFrame = Time.frameCount;
                cell.EndTime = Time.realtimeSinceStartup;
                cell.State = isSucc ? CellState.Succ : CellState.Fail;
            }
        }

        public void UnloadAsset(AssetInfo info)
        {
            UnloadCell cell = null;
            if (!UnloadAssetCells.TryGetValue(info, out cell))
            {
                cell = new UnloadCell();
                UnloadAssetCells[info] = cell;
                cell.Date = DateTime.Now;
                cell.StartFrame = Time.frameCount;
                cell.StartTime = Time.realtimeSinceStartup;
                cell.AssetName = info.AssetName;
                cell.BundleName = info.BundleName;
            }
        }

        public void UnloadBundle(string bundle)
        {
            UnloadCell cell = null;
            if (!UnloadBundleCells.TryGetValue(bundle, out cell))
            {
                cell = new UnloadCell();
                UnloadBundleCells[bundle] = cell;
                cell.Date = DateTime.Now;
                cell.StartFrame = Time.frameCount;
                cell.StartTime = Time.realtimeSinceStartup;
                cell.BundleName = bundle;
            }
        }

        private void SaveUnloadAsset(string file)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append("DateTime,");
            strBuilder.Append("LoadStartFrameCount,");
            strBuilder.Append("LoadStartTime,");
            strBuilder.Append("BundleName,");
            strBuilder.Append("AssetName,");
            strBuilder.AppendLine();

            foreach (var pair in UnloadAssetCells)
            {
                var assetCell = pair.Value;
                strBuilder.Append(string.Format("{0:yyyy_M_d_HH_mm_ss}", assetCell.Date));
                strBuilder.Append(",");
                strBuilder.Append(assetCell.StartFrame);
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}", assetCell.StartTime));
                strBuilder.Append(",");
                strBuilder.Append(assetCell.AssetName);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.BundleName);
                strBuilder.Append(",");
                strBuilder.AppendLine();
            }

            var text = strBuilder.ToString();
            File.WriteAllText(file, text);
        }

        private void SaveUnloadBundle(string file)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append("DateTime,");
            strBuilder.Append("LoadStartFrameCount,");
            strBuilder.Append("LoadStartTime,");
            strBuilder.Append("BundleName,");
            strBuilder.Append("UnloadTime,");
            strBuilder.AppendLine();

            var cloneList = GetUnloadBundleCellClone();

            for (int i = 0; i < cloneList.Count; i++)
            {
                var assetCell = cloneList[i];
                UnloadCell nextCell = null;
                if(i < cloneList.Count - 1)
                    nextCell = cloneList[i + 1];

                strBuilder.Append(string.Format("{0:yyyy_M_d_HH_mm_ss}", assetCell.Date));
                strBuilder.Append(",");
                strBuilder.Append(assetCell.StartFrame);
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}", assetCell.StartTime));
                strBuilder.Append(",");
                strBuilder.Append(assetCell.BundleName);
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}s", nextCell == null ? 0 : nextCell.StartTime - assetCell.StartTime));
                strBuilder.Append(",");
                strBuilder.AppendLine();
            }

            var text = strBuilder.ToString();
            File.WriteAllText(file, text);
        }

        private void SaveUnloadAll(string file)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append("DateTime,");
            strBuilder.Append("LoadStartFrameCount,");
            strBuilder.Append("LoadStartTime,");
            strBuilder.Append("Type,");
            strBuilder.Append("BundleName,");
            strBuilder.Append("AssetName,");
            strBuilder.AppendLine();

            var assetClone = GetUnloadAssetCellClone();
            var bundleClone = GetUnloadBundleCellClone();
            bool isComplete = false;
            while (!isComplete)
            {
                if (assetClone.Count == 0 && bundleClone.Count == 0)
                {
                    isComplete = true; continue;
                }

                UnloadCell nextAssetCell = null;
                if (assetClone.Count > 0)
                    nextAssetCell = assetClone[0];
                UnloadCell nextBundleCell = null;
                if (bundleClone.Count > 0)
                    nextBundleCell = bundleClone[0];

                if (nextAssetCell == null || (nextBundleCell != null && nextBundleCell.StartFrame <= nextAssetCell.StartFrame))
                {
                    bundleClone.Remove(nextBundleCell);
                    strBuilder.Append(string.Format("{0:yyyy_M_d_HH_mm_ss}", nextBundleCell.Date));
                    strBuilder.Append(",");
                    strBuilder.Append(nextBundleCell.StartFrame);
                    strBuilder.Append(",");
                    strBuilder.Append(string.Format("{0:0.000}", nextBundleCell.StartTime));
                    strBuilder.Append(",");
                    strBuilder.Append("BundleType");
                    strBuilder.Append(",");
                    strBuilder.Append(nextBundleCell.BundleName);
                    strBuilder.Append(",");
                    strBuilder.Append("/");
                    strBuilder.Append(",");
                }
                else
                {
                    assetClone.Remove(nextAssetCell);
                    strBuilder.Append(string.Format("{0:yyyy_M_d_HH_mm_ss}", nextAssetCell.Date));
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.StartFrame);
                    strBuilder.Append(",");
                    strBuilder.Append(string.Format("{0:0.000}", nextAssetCell.StartTime));
                    strBuilder.Append(",");
                    strBuilder.Append("AssetType");
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.BundleName);
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.AssetName);
                    strBuilder.Append(",");
                }
                strBuilder.AppendLine();
            }

            var text = strBuilder.ToString();
            File.WriteAllText(file, text);
        }

        private void SaveAsset(string file)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append("DateTime,");
            strBuilder.Append("AddQueueFrameCount,");
            strBuilder.Append("AddQueueTime,");
            strBuilder.Append("BundleName,");
            strBuilder.Append("AssetName,");
            strBuilder.Append("AssetType,");
            strBuilder.Append("IsSyn,");
            strBuilder.Append("IsAsset,");
            strBuilder.Append("Priority,");
            strBuilder.Append("LoadState,");
            strBuilder.Append("LoadStartFrameCount,");
            strBuilder.Append("LoadEndFrameCount,");
            strBuilder.Append("LoadStartTime,");
            strBuilder.Append("LoadEndTime,");
            strBuilder.Append("LoadCostAllTime,");
            strBuilder.AppendLine();

            foreach (var pair in AssetCells)
            {
                var assetCell = pair.Value;
                strBuilder.Append(string.Format("{0:yyyy_M_d_HH_mm_ss}", assetCell.Date));
                strBuilder.Append(",");
                strBuilder.Append(assetCell.QueueAddStartFrame);
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}", assetCell.QueueAddStartTime));
                strBuilder.Append(",");
                strBuilder.Append(assetCell.BundleName);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.AssetName);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.AssetType);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.IsAsync ? "Async" : "Sync");
                strBuilder.Append(",");
                strBuilder.Append(assetCell.RecAssetType);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.PriorityType);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.State);
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}", assetCell.LoadStartFrame));
                strBuilder.Append(",");
                strBuilder.Append(assetCell.LoadEndFrame);
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}", assetCell.LoadStartTime));
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}", assetCell.LoadEndTime));
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}s", assetCell.LoadEndTime - assetCell.LoadStartTime));
                strBuilder.Append(",");
                strBuilder.AppendLine();
            }

            var text = strBuilder.ToString();
            File.WriteAllText(file, text);
        }

        private void SaveAll(string file)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append("DateTime,");
            strBuilder.Append("AddQueueFrameCount,");
            strBuilder.Append("AddQueueStartTime,");
            strBuilder.Append("Type,");
            strBuilder.Append("BundleName,");
            strBuilder.Append("AssetName,");
            strBuilder.Append("AssetType,");
            strBuilder.Append("IsSyn,");
            strBuilder.Append("IsAsset,");
            strBuilder.Append("Priority,");
            strBuilder.Append("LoadState,");
            strBuilder.Append("LoadStartFrameCount,");
            strBuilder.Append("LoadEndFrameCount,");
            strBuilder.Append("LoadStartTime,");
            strBuilder.Append("LoadEndTime,");
            strBuilder.Append("LoadCostAllTime,");
            strBuilder.AppendLine();

            var assetClone = GetAssetCellClone();
            var bundleClone = GetBundleCellClone();
            bool isComplete = false;
            while (!isComplete)
            {
                if (assetClone.Count == 0 && bundleClone.Count == 0)
                {
                    isComplete = true;continue;
                }

                LoadAssetCell nextAssetCell = null;
                if (assetClone.Count > 0)
                    nextAssetCell = assetClone[0];
                LoadBundleCell nextBundleCell = null;
                if (bundleClone.Count > 0)
                    nextBundleCell = bundleClone[0];

                if (nextAssetCell == null || (nextBundleCell != null && nextBundleCell.StartFrame <= nextAssetCell.QueueAddStartFrame))
                {
                    bundleClone.Remove(nextBundleCell);
                    strBuilder.Append(string.Format("{0:yyyy_M_d_HH_mm_ss}", nextBundleCell.Date));
                    strBuilder.Append(",");
                    strBuilder.Append("/");
                    strBuilder.Append(",");
                    strBuilder.Append("/");
                    strBuilder.Append(",");
                    strBuilder.Append("BundleType");
                    strBuilder.Append(",");
                    strBuilder.Append(nextBundleCell.BundleName);
                    strBuilder.Append(",");
                    strBuilder.Append("/");
                    strBuilder.Append(",");
                    strBuilder.Append("/");
                    strBuilder.Append(",");
                    strBuilder.Append("/");
                    strBuilder.Append(",");
                    strBuilder.Append("/");
                    strBuilder.Append(",");
                    strBuilder.Append("/");
                    strBuilder.Append(",");
                    strBuilder.Append(nextBundleCell.State);
                    strBuilder.Append(",");
                    strBuilder.Append(nextBundleCell.StartFrame);
                    strBuilder.Append(",");
                    strBuilder.Append(nextBundleCell.EndFrame);
                    strBuilder.Append(",");
                    strBuilder.Append(string.Format("{0:0.000}", nextBundleCell.StartTime));
                    strBuilder.Append(",");
                    strBuilder.Append(string.Format("{0:0.000}", nextBundleCell.EndTime));
                    strBuilder.Append(",");
                    strBuilder.Append(string.Format("{0:0.000}s", nextBundleCell.EndTime - nextBundleCell.StartTime));
                    strBuilder.Append(",");
                }
                else
                {
                    assetClone.Remove(nextAssetCell);
                    strBuilder.Append(string.Format("{0:yyyy_M_d_HH_mm_ss}", nextAssetCell.Date));
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.QueueAddStartFrame);
                    strBuilder.Append(",");
                    strBuilder.Append(string.Format("{0:0.000}", nextAssetCell.QueueAddStartTime));
                    strBuilder.Append(",");
                    strBuilder.Append("AssetType");
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.BundleName);
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.AssetName);
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.AssetType);
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.IsAsync ? "Async" : "Sync");
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.RecAssetType);
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.PriorityType);
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.State);
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.LoadStartFrame);
                    strBuilder.Append(",");
                    strBuilder.Append(nextAssetCell.LoadEndFrame);
                    strBuilder.Append(",");
                    strBuilder.Append(string.Format("{0:0.000}", nextAssetCell.LoadStartTime));
                    strBuilder.Append(",");
                    strBuilder.Append(string.Format("{0:0.000}", nextAssetCell.LoadEndTime));
                    strBuilder.Append(",");
                    strBuilder.Append(string.Format("{0:0.000}s", nextAssetCell.LoadEndTime - nextAssetCell.LoadStartTime));
                    strBuilder.Append(",");
                }
                strBuilder.AppendLine();
            }

            var text = strBuilder.ToString();
            File.WriteAllText(file, text);
        }

        private void SaveBundle(string file)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append("DateTime,");
            strBuilder.Append("LoadStartFrameCount,");
            strBuilder.Append("LoadEndFrameCount,");
            strBuilder.Append("BundleName,");
            strBuilder.Append("State,");
            strBuilder.Append("LoadStartTime,");
            strBuilder.Append("LoadEndTime,");
            strBuilder.Append("LoadCostAllTime,");
            strBuilder.AppendLine();

            foreach (var pair in BundleCells)
            {
                var assetCell = pair.Value;
                strBuilder.Append(string.Format("{0:yyyy_M_d_HH_mm_ss}", assetCell.Date));
                strBuilder.Append(",");
                strBuilder.Append(assetCell.StartFrame);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.EndFrame);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.BundleName);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.State);
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}", assetCell.StartTime));
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}", assetCell.EndTime));
                strBuilder.Append(",");
                strBuilder.Append(string.Format("{0:0.000}s", assetCell.EndTime - assetCell.StartTime));
                strBuilder.Append(",");
                strBuilder.AppendLine();
            }

            var text = strBuilder.ToString();
            File.WriteAllText(file, text);
        }

        private void SaveRef(string file)
        {
            var strBuilder = new StringBuilder();
            strBuilder.Append("BundleName,");
            strBuilder.Append("AssetName,");
            strBuilder.Append("RefCount,");
            strBuilder.Append("LoadState,");
            strBuilder.Append("CannotDestroy,");
            strBuilder.AppendLine();

            foreach (var pair in RefAssetCells)
            {
                var assetCell = pair;
                strBuilder.Append(assetCell.BundleName);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.AssetName);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.RefCount);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.LoadState);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.CannotDestroy);
                strBuilder.Append(",");
                strBuilder.AppendLine();
            }

            foreach (var pair in RefBundleCells)
            {
                var assetCell = pair;
                strBuilder.Append(assetCell.BundleName);
                strBuilder.Append(",");
                strBuilder.Append("/");
                strBuilder.Append(",");
                strBuilder.Append(assetCell.RefCount);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.LoadState);
                strBuilder.Append(",");
                strBuilder.Append(assetCell.CannotDestroy);
                strBuilder.Append(",");
                strBuilder.AppendLine();
            }

            var text = strBuilder.ToString();
            File.WriteAllText(file, text);
        }

        public void ClearBefore(string key)
        {
            var limit = ResourceConfigure.AssetBundleRecorderDirLimitCount;
            var dirs = Directory.GetDirectories("replay");
            List<string> getDirs = new List<string>();
            for (int i = 0; i < dirs.Length; i++)
            {
                if (Path.GetFileName(dirs[i]).StartsWith(key))
                {
                    getDirs.Add(dirs[i]);
                }
            }

            if (getDirs.Count >= limit)
            {
                var prefix = key + "_AssetBundleRecorder_";
                var dateTimes = new List<DateTime>();
                for (int i = 0; i < getDirs.Count; ++i)
                {
                    var filename = Path.GetFileName(getDirs[i]);
                    var dateStr = filename.Replace(prefix, "");
                    var dateTime = DateTime.ParseExact(dateStr, "yyyy_M_d_HH_mm_ss", System.Globalization.CultureInfo.InvariantCulture);
                    dateTimes.Add(dateTime);
                }

                dateTimes.Sort((x, y) => DateTime.Compare(x, y));
                for (int i = 0; i < dateTimes.Count - limit; ++i)
                {
                    var time = string.Format("{0:yyyy_M_d_HH_mm_ss}", dateTimes[i]);
                    var dirname = string.Format("{0}{1}", prefix, time);
                    var dirPath = Path.Combine("replay", dirname);
                    Directory.Delete(dirPath, true);
                }
            }
        }

        public void Save(string key)
        {
            ClearBefore(key);

            var dir = Path.Combine("replay", string.Format("{2}_{1}_{0:yyyy_M_d_HH_mm_ss}", DateTime.Now, "AssetBundleRecorder", key));

            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            var assetFile = Path.Combine(dir, string.Format("AssetLoad.csv"));
            SaveAsset(assetFile);

            var bundleFile = Path.Combine(dir, string.Format("BundleLoad.csv"));
            SaveBundle(bundleFile);

            var allFile = Path.Combine(dir, string.Format("AllLoad.csv"));
            SaveAll(allFile);

            var unloadassetFile = Path.Combine(dir, string.Format("AssetUnLoad.csv"));
            SaveUnloadAsset(unloadassetFile);

            var unloadbundleFile = Path.Combine(dir, string.Format("BundleUnLoad.csv"));
            SaveUnloadBundle(unloadbundleFile);

            var unloadallFile = Path.Combine(dir, string.Format("AllUnLoad.csv"));
            SaveUnloadAll(unloadallFile);

            var refFile = Path.Combine(dir, string.Format("AllRef.csv"));
            SaveRef(refFile);

            Clear();
        }

        public void Clear()
        {
            UnloadAssetCells.Clear();
            UnloadBundleCells.Clear();
            AssetCells.Clear();
            BundleCells.Clear();
            RefAssetCells.Clear();
            RefBundleCells.Clear();
        }
    }
}