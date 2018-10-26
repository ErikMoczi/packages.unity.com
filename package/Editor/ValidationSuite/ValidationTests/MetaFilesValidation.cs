using System;
using System.IO;

namespace UnityEditor.PackageManager.ValidationSuite.ValidationTests
{
    internal class MetaFilesValidation : BaseValidation
    {
        public MetaFilesValidation()
        {
            TestName = "Meta Files Validation";
            TestDescription = "Validate that metafiles are present for all package files.";
            TestCategory = TestCategory.ContentScan;
        }

        bool ShouldIgnore(string name)
        {
            //Names starting with a "." are being ignored by AssetDB.
            //Names finishing with ".meta" are considered meta files in Editor Code.
            if(Path.GetFileName(name).StartsWith(".") || name.EndsWith(".meta"))
                return true;

            // Honor the Unity tilde skipping of import
            if (Path.GetDirectoryName(name).EndsWith("~") || name.EndsWith("~"))
                return true;

            return false;
        }
        
        void CheckMeta(string toCheck)
        {
            if(ShouldIgnore(toCheck))
                return;
            
            if(System.IO.File.Exists(toCheck + ".meta"))
                return;
            
            TestState = TestState.Failed;
            TestOutput.Add("Did not find meta file for " + toCheck);
        }


        void CheckMetaInFolderRecursively(string folder)
        {
            try
            {
                foreach (string file in Directory.GetFiles(folder))
                {
                    CheckMeta(file);
                }

                foreach (string dir in Directory.GetDirectories(folder))
                {
                    if(ShouldIgnore(dir))
                        continue;
                    
                    CheckMeta(dir);
                    CheckMetaInFolderRecursively(dir);
                }
            }
            catch (Exception e)
            {
                TestState = TestState.Failed;
                TestOutput.Add("Exception " + e.Message);
            }
        }

        protected override void Run()
        {
            // Start by declaring victory
            TestState = TestState.Succeeded;

            //check if each file/folder has its .meta counter-part
            CheckMetaInFolderRecursively(Context.PublishPackageInfo.path);
        }
    }
}