using UnityEngine;

namespace TestUtils
{
    public static class TestFilePath
    {
        public static readonly string Folder =
            Application.dataPath + "../../../Framework/Tests/Runtime/Standard/FileTestFolder/";

        public static readonly string TestFile1 = 
            Application.dataPath + "../../../Framework/Tests/Runtime/Standard/FileTestFolder/testFile1.txt";

        public static readonly string TestFile2 =
            Application.dataPath + "../../../Framework/Tests/Runtime/Standard/FileTestFolder/testFile2.txt";
    }
}
