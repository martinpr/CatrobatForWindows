﻿using System;
using System.IO;
using System.Xml.Linq;
using Catrobat.Core;
using Catrobat.Core.Objects;
using Catrobat.Core.Storage;
using Catrobat.Core.VersionConverter;
using Catrobat.TestsCommon.Misc;
using Catrobat.TestsCommon.Misc.Storage;
using Catrobat.TestsCommon.SampleData;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Catrobat.TestsCommon.Tests.Data
{
    [TestClass]
    public class DataReadingTests
    {
        [ClassInitialize]
        public static void TestClassInitialize(TestContext testContext)
        {
            TestHelper.InitializeTests();
        }

        [TestMethod]
        public void DataReadingTest1()
        {
            XDocument xDocument = SampleLoader.LoadSampleXDocument("DataReadingTests/test_code1");
            CatrobatVersionConverter.Convert("0.8", "Win0.8", xDocument);

            var writer = new XmlStringWriter();
            xDocument.Save(writer, SaveOptions.None);

            var xml = writer.GetStringBuilder().ToString();
            var project = new Project(xml);

            // TODO: check project if it is correct!
        }
    }
}
