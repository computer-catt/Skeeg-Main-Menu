dotnet publish"" -c Release -o ./dist 
set ModFolder "/home/ComputerCat/.config/r2modmanPlus-local/REPO/profiles/MEOW/BepInEx/plugins/MainMenuSillyCatSpam/"
cp ./dist/SkeegMainMenu.dll $ModFolder
cp ./UnityProject/Assets/AssetBundles/skeeg $ModFolder
exit