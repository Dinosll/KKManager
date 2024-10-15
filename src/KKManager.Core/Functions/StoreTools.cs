using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization.Configuration;
using KKManager.Data.Cards;
using KKManager.Properties;

namespace KKManager.Functions
{
    public static class StoreTools
    {
        private static CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        private static DirectoryInfo _workingDirectory;
        public static void StoreUnsortedCards(bool debug)
        {
            _workingDirectory = SelectUnsortedCardsPath();
            if (_workingDirectory == null)
            {
                Console.WriteLine("Cancel sort cards");
                return;
            }
            Console.WriteLine("CatchPath:"+_workingDirectory.FullName);
            Parallel.ForEach(_workingDirectory.EnumerateFiles("*.png", SearchOption.AllDirectories)
                , new ParallelOptions { CancellationToken = _cancellationTokenSource.Token }
                , file =>
                {
                    if (CardLoader.TryParseCard(file, out var card))
                    {
                        //isGameCard
                        CopyCardsToGameInstallFolder(card,file);
                    }
                    else
                    {
                        //MaybeIsOutsideTexture
                        CopyFileToCustomFolder(file);
                    }
                });

            Parallel.ForEach(_workingDirectory.EnumerateFiles("*.zipmod", SearchOption.AllDirectories)
                , new ParallelOptions { CancellationToken = _cancellationTokenSource.Token }
                , file =>
                {
                    //isAdditionMods
                    CopyModsToGameInstallFolder(file);
                });

            _workingDirectory = null;
        }
            
        public static DirectoryInfo SelectUnsortedCardsPath()
        {
            var result = MessageBox.Show(string.Format(Resources.SelectUnstoredFolderTip),"Select Folder"
                ,MessageBoxButtons.OKCancel, MessageBoxIcon.Information);
            if (result == DialogResult.OK)
            {
                using (var d = new FolderBrowserDialog())
                {
                    if (d.ShowDialog() == DialogResult.OK)
                    {
                        if(MessageBox.Show($"Select {d.SelectedPath}?"
                               ,"Confirm",MessageBoxButtons.OKCancel,MessageBoxIcon.Warning)==DialogResult.OK)
                            return new DirectoryInfo(d.SelectedPath);
                    }
                }   
            }

            return null;
        }

        private static void CopyCardsToGameInstallFolder(Card card,FileInfo file)
        {
            var gameInstallPath = Settings.Default.GamePath;
            var destPath = string.Empty;
            switch (card.Type)
            {
                case CardType.Koikatu:
                case CardType.KoikatsuSunshine://SameAsKoikatu
                    destPath = GetSavePath(SortFileType.KoikatuCharaCard, file.FullName);
                    break;
                case CardType.KoikatuClothes:
                    destPath = GetSavePath(SortFileType.KoikatuCoordinateCard, file.FullName);
                    break;
                case CardType.KoikatuStudioScene:
                    destPath = GetSavePath(SortFileType.KoikatuStudioSceneCard,file.FullName);
                    break;
                default:
                    Console.WriteLine($"Unknow GameType {card.Type},Don't Copy {card.Name}");
                    return;
            }

            if (File.Exists(destPath))
            {
                CopyFileToExistFloder(file);
                return;
            }

            CopyToPath(file,destPath);
        }

        private static void CopyFileToCustomFolder(FileInfo file)
        {
            var destPath=GetSavePath(SortFileType.UnknownTypeCard,file.FullName);
            CopyToPath(file,destPath);
        }
        
        private static void CopyModsToGameInstallFolder(FileInfo file)
        {
            var destPath = GetSavePath(SortFileType.AdditionMod,file.FullName);
            CopyToPath(file,destPath);
        }

        private static void CopyFileToExistFloder(FileInfo file)
        {
            var destPath = GetSavePath(SortFileType.ExistSameFile,file.FullName);
            CopyToPath(file,destPath);
        }

        private static void CopyToPath(FileInfo file, string destPath)
        {
            
            try
            {
                var destDirectoryPath = Path.GetDirectoryName(destPath);
                if(destDirectoryPath!=string.Empty && !Directory.Exists(destDirectoryPath))
                    Directory.CreateDirectory(destDirectoryPath);
                File.Copy(file.FullName, destPath);

            }
            catch (Exception e)
            {
                Console.WriteLine($"Path:{destPath} not valid");
                throw;
            }
        }

        private static string GetSavePath(SortFileType type, string fileFullPath)
        {
            var fileRelativePath = fileFullPath.Replace(_workingDirectory.FullName, "");
            var customPath = Path.Combine(Settings.Default.GamePath
                ,DateTime.Now.ToString(@"yyMMdd")+"-UnknownTypeFiles");
            switch (type)
            {
                case SortFileType.KoikatuCharaCard:
                    customPath=Path.Combine(Settings.Default.GamePath
                        , "UserData"
                        , "chara"
                        , "female"
                        , DateTime.Now.ToString(@"yyMMdd"));
                    break;
                case SortFileType.KoikatuCoordinateCard:
                    customPath=Path.Combine(Settings.Default.GamePath
                        , "UserData"
                        , "coordinate"
                        , DateTime.Now.ToString(@"yyMMdd"));
                    break;
                case SortFileType.KoikatuStudioSceneCard:
                    customPath = Path.Combine(Settings.Default.GamePath
                        , "UserData"
                        , "studio"
                        , "scene"
                        , DateTime.Now.ToString("yyMMdd"));
                    break;
                case SortFileType.UnknownTypeCard:
                    customPath = Path.Combine(Settings.Default.GamePath
                        , DateTime.Now.ToString("yyMMdd"))+"_UnknownFileType";
                    break;
                case SortFileType.AdditionMod:
                    customPath = Path.Combine(Settings.Default.GamePath
                        , "mods"
                        , "AdditionZipmod"
                        , DateTime.Now.ToString("yyMMdd"));
                    break;
                case SortFileType.ExistSameFile:
                    customPath = Path.Combine(Settings.Default.GamePath
                        , DateTime.Now.ToString("yyMMdd"))+"_ExistSameFile";
                    break;
            }

            return customPath + fileRelativePath;
        }
    }

    public enum SortFileType
    {
        None,
        KoikatuCharaCard,
        KoikatuCoordinateCard,
        KoikatuStudioSceneCard,
        UnknownTypeCard,
        AdditionMod,
        ExistSameFile,
    }
}