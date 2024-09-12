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
        public static void StoreUnsortedCards(bool debug)
        {
            var selectedFolderPath = SelectUnsortedCardsPath();
            Console.WriteLine("CatchPath:"+selectedFolderPath.FullName);
            Parallel.ForEach(selectedFolderPath.EnumerateFiles("*.png", SearchOption.AllDirectories)
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

            Parallel.ForEach(selectedFolderPath.EnumerateFiles("*.zipmod", SearchOption.AllDirectories)
                , new ParallelOptions { CancellationToken = _cancellationTokenSource.Token }
                , file =>
                {
                    //isAdditionMods
                    CopyModsToGameInstallFolder(file);
                });
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
                    if (!Directory.Exists(charaFolderPath))
                        Directory.CreateDirectory(charaFolderPath);
                    destPath = Path.Combine(charaFolderPath, file.Name);
                    
                    break;
                case CardType.KoikatuClothes:
                    var coordinateFolderPath=Path.Combine(gameInstallPath
                        , "UserData"
                        , "coordinate"
                        , DateTime.Now.ToString(@"yyMMdd"));
                    if (!Directory.Exists(coordinateFolderPath))
                        Directory.CreateDirectory(coordinateFolderPath);
                    destPath = Path.Combine(coordinateFolderPath, file.Name);
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

            try
            {
                File.Copy(file.FullName, destPath);
            }
            catch (Exception e)
            {
                Console.WriteLine("invalid destPath"+destPath);
                throw;
            }
        }

        private static void CopyFileToCustomFolder(FileInfo file)
        {
            var customPath = Path.Combine(Settings.Default.GamePath
                ,DateTime.Now.ToString(@"yyMMdd")+"未分类");
            if (!Directory.Exists(customPath))
                Directory.CreateDirectory(customPath);
            var destPath = Path.Combine(customPath, Path.GetFileName(file.Name));
            if (File.Exists(destPath))
            {
                CopyFileToExistFloder(file);
                return;
            }
            File.Copy(file.FullName, destPath);
        }
        
        private static void CopyModsToGameInstallFolder(FileInfo file)
        {
            var customPath = Path.Combine(Settings.Default.GamePath
                ,"mods"
                ,"AdditionZipmod"
                ,DateTime.Now.ToString(@"yyMMdd"));
            if (!Directory.Exists(customPath))
                Directory.CreateDirectory(customPath);
            var destPath = Path.Combine(customPath, Path.GetFileName(file.Name));
            if (File.Exists(destPath))
            {
                CopyFileToExistFloder(file);
                return;
            }
            File.Copy(file.FullName, destPath);
        }

        private static void CopyFileToExistFloder(FileInfo file)
        {
            var customPath = Path.Combine(Settings.Default.GamePath
                ,DateTime.Now.ToString(@"yyMMdd")+"名称重复");
            if (!Directory.Exists(customPath))
                Directory.CreateDirectory(customPath);
            var destPath = Path.Combine(customPath, Path.GetFileName(file.Name));
            if (File.Exists(destPath))
                destPath = Path.Combine(customPath,  Guid.NewGuid().ToString().Substring(0,8)+"_"+Path.GetFileName(file.Name));
            File.Copy(file.FullName, destPath);
        }
    }
}