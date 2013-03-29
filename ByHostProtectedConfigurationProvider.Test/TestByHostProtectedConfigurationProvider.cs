#region Coypright and License

/*
 * ByHostProtectedConfigurationProvider - Copyright 2013, Svante Seleborg, All Rights Reserved
 *
 * This file is part of ByHostProtectedConfigurationProvider.
 *
 * ByHostProtectedConfigurationProvider is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 *
 * ByHostProtectedConfigurationProvider is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 *
 * You should have received a copy of the GNU General Public License
 * along with ByHostProtectedConfigurationProvider.  If not, see <http://www.gnu.org/licenses/>.
 *
 * The source is maintained at https://byhostconfiguration.codeplex.com/ please visit for
 * updates, contributions and contact with the author. You may also visit
 * http://www.axantum.com for more information about the author.
*/

#endregion Coypright and License

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Xml;

using Axantum.Xecrets.Providers;

using NUnit.Framework;

namespace Axantum.Xecrets.Providers.Test
{
    [TestFixture]
    public class TestByHostProtectedConfigurationProvider
    {
        private ByHostProtectedConfigurationProvider _provider;

        [NUnit.Framework.TestFixtureSetUp]
        public void InitializeProvider()
        {
            _provider = new ByHostProtectedConfigurationProvider();

            NameValueCollection config = new NameValueCollection();
            config.Add("hostName", "testhost");

            _provider.Initialize("TestByHostSpecificProvider", config);
        }

        [NUnit.Framework.Test]
        public void TestProvider()
        {
            string testSection = @"
                <EncryptedData>
                    <host>
                        <mailSettings>
                            <smtp from=""Axantum Software AB &lt;register@axantum.com>"">
                                <network host=""smtp.axantum.com""
                                         port=""25""
                                         />
                            </smtp>
                        </mailSettings>
                    </host>
                    <host hostName=""testhost"">
                        <mailSettings>
                            <smtp>
                                <network password=""Passw0rd!""
                                         port=""12325""
                                         userName=""auser""
                                         />
                            </smtp>
                        </mailSettings>
                    </host>
                </EncryptedData>
            ";

            string expected = @"<smtp from=""Axantum Software AB &lt;register@axantum.com&gt;""><network host=""smtp.axantum.com"" port=""12325"" password=""Passw0rd!"" userName=""auser"" /></smtp>";

            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.LoadXml(testSection);

            XmlNode testNode = xmlDocument.DocumentElement;

            XmlNode resultNode = _provider.Decrypt(testNode);

            Assert.That(resultNode.InnerXml, Is.EqualTo(expected), "Merge");
        }
    }
}
