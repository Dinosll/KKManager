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
            MessageBox.Show("Select Unsorted CardsFolder"
                ,"Select Unsorted Cards Folder"
                ,MessageBoxButtons.OK, MessageBoxIcon.Error);
            using (var d = new FolderBrowserDialog())
            {
                if (d.ShowDialog() == DialogResult.OK)
                {
                    return new DirectoryInfo(d.SelectedPath);
                }
                return null;
            }   
        }

        private static void CopyCardsToGameInstallFolder(Card card,FileInfo file)
        {
            var gameInstallPath = Settings.Default.GamePath;
            var destPath = string.Empty;
            switch (card.Type)
            {
                case CardType.Koikatu:
                case CardType.KoikatsuSunshine://SameAsKoikatu
                    var charaFolderPath=Path.Combine(gameInstallPath
                        , "UserData"
                        , "chara"
                        , "female"
                        , DateTime.Now.ToString(@"yyMMdd"));
                    destPath = Path.Combine(charaFolderPath, file.FullName.Replace(_workingDirectory.FullName,""));
                    break;
                case CardType.KoikatuClothes:
                    var coordinateFolderPath=Path.Combine(gameInstallPath
                        , "UserData"
                        , "coordinate"
                        , DateTime.Now.ToString(@"yyMMdd"));
                    destPath = Path.Combine(coordinateFolderPath, file.FullName.Replace(_workingDirectory.FullName,""));
                    break;
                case CardType.KoikatuStudioScene:
                    var sceneCardPath = Path.Combine(gameInstallPath
                        , "UserData"
                        , "studio"
                        , "scene"
                        , DateTime.Now.ToString("yyMMdd"));
                    if (!Directory.Exists(sceneCardPath))
                        Directory.CreateDirectory(sceneCardPath);
                    destPath = Path.Combine(sceneCardPath, file.FullName.Replace(_workingDirectory.FullName, ""));
                    break;
                default:
                    Console.WriteLine($"Don't Copy {card.Name}");
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
            var customPath = Path.Combine(Settings.Default.GamePath
                ,DateTime.Now.ToString(@"yyMMdd")+"未分类");
            var destPath = Path.Combine(customPath, file.FullName.Replace(_workingDirectory.FullName,""));
            CopyToPath(file,destPath);
        }
        
        private static void CopyModsToGameInstallFolder(FileInfo file)
        {
            var customPath = Path.Combine(Settings.Default.GamePath
                ,"mods"
                ,"AdditionZipmod"
                ,DateTime.Now.ToString(@"yyMMdd"));
            var destPath = Path.Combine(customPath, file.FullName.Replace(_workingDirectory.FullName,""));
            CopyToPath(file,destPath);
        }

        private static void CopyFileToExistFloder(FileInfo file)
        {
            var customPath = Path.Combine(Settings.Default.GamePath
                ,DateTime.Now.ToString(@"yyMMdd")+"名称重复");
            var destPath = Path.Combine(customPath, file.FullName.Replace(_workingDirectory.FullName,""));
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
    }
}