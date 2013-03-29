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
using System.Collections.Specialized;
using System.Configuration;
using System.Configuration.Provider;
using System.Globalization;
using System.Xml;

namespace Axantum.Xecrets.Providers
{
    /// <summary>
    /// Implements a host specific ProtectedConfigurationProvider, which may return different data
    /// and have different behavior depending on the actual host that is running the code. This is
    /// intended to make it easier to maintain and deploy code between development and different
    /// deployment scenarios.
    /// The implementation piggy-backs the ProtectedConfigurationProvider infrastructure. In the future
    /// it should also support nesting with a 'Real' ProtectedConfigurationProvider so that it can
    /// actually protect as well.
    /// </summary>
    public class ByHostProtectedConfigurationProvider : ProtectedConfigurationProvider
    {
        private enum Mode
        {
            Merge,
            Append,
            AddRemoveClear,
        }

        private string _hostName;

        public override void Initialize(string name, NameValueCollection config)
        {
            if (config == null)
            {
                throw new ArgumentNullException("config");
            }

            if (string.IsNullOrEmpty(config["name"]))
            {
                name = "ByHostProtectedConfigurationProvider";
            }

            if (string.IsNullOrEmpty(config["description"]))
            {
                config.Remove("description");
                config.Add("description", "Configuration by Host Protected Configuration Provider");
            }

            base.Initialize(name, config);

            if (string.IsNullOrEmpty(config["hostName"]))
            {
                _hostName = System.Net.Dns.GetHostName();
            }
            else
            {
                _hostName = config["hostName"];
            }

            config.Remove("hostName");

            if (config.Count > 0)
            {
                string unknownValue = config.GetKey(0);

                if (!string.IsNullOrEmpty(unknownValue))
                {
                    throw new ProviderException("Unrecognized Configuration Attribute: " + unknownValue);
                }
            }
        }

        public override XmlNode Decrypt(XmlNode encryptedNode)
        {
            if (encryptedNode == null)
            {
                throw new ArgumentNullException("encryptedNode");
            }

            XmlNodeList hostDataList = encryptedNode.SelectNodes("/EncryptedData/host");
            XmlNode mergedNode = null;
            foreach (XmlNode xmlNode in hostDataList)
            {
                if (xmlNode.ChildNodes.Count != 1)
                {
                    string exeptionMessage = String.Format(CultureInfo.InvariantCulture, "There must be exactly one element in the <hostData> element. There are {0}.", xmlNode.ChildNodes.Count);
                    throw new XmlException(exeptionMessage);
                }

                XmlAttribute modeAttribute = xmlNode.Attributes.GetNamedItem("mode") as XmlAttribute;
                Mode mode = Mode.Merge;
                if (modeAttribute != null && modeAttribute.Value.Length > 0)
                {
                    string modeAttributeValue = modeAttribute.Value;
                    if (modeAttributeValue == "append")
                    {
                        mode = Mode.Append;
                    }
                    else if (modeAttributeValue == "arc")
                    {
                        mode = Mode.AddRemoveClear;
                    }
                    else if (modeAttributeValue != "merge")
                    {
                        string exeptionMessage = String.Format(CultureInfo.InvariantCulture, "The value of the mode attribute must be 'append' or 'merge', it is '{0}'.", modeAttributeValue);
                        throw new XmlException(exeptionMessage);
                    }
                }

                XmlAttribute hostNameAttribute = xmlNode.Attributes.GetNamedItem("hostName") as XmlAttribute;
                if (hostNameAttribute == null || hostNameAttribute.Value.Length == 0 || hostNameAttribute.Value == _hostName)
                {
                    XmlNode newNode = xmlNode.FirstChild;
                    switch (mode)
                    {
                        case Mode.Merge:
                            mergedNode = XmlMerge(mergedNode, newNode);
                            break;
                        case Mode.Append:
                            mergedNode = XmlAppend(mergedNode, newNode);
                            break;
                        case Mode.AddRemoveClear:
                            mergedNode = XmlAddRemoveClear(mergedNode, newNode);
                            break;
                        default:
                            throw new ProviderException("Invalide mode enumeration.");
                    }
                }
            }
            return mergedNode;
        }

        public override XmlNode Encrypt(XmlNode node)
        {
            throw new NotImplementedException("The method or operation is not implemented.");
        }

        private static XmlNode XmlAppend(XmlNode currentNode, XmlNode newNode)
        {
            if (newNode == null)
            {
                return currentNode;
            }

            if (currentNode == null)
            {
                return newNode;
            }

            foreach (XmlNode newChildNode in newNode.ChildNodes)
            {
                // Must make a Clone() here to ensure that we really append, otherwise we might remove and add to the end.
                currentNode.AppendChild(newChildNode.Clone());
            }
            return currentNode;
        }

        private XmlNode XmlMerge(XmlNode currentNode, XmlNode newNode)
        {
            if (newNode == null)
            {
                return currentNode;
            }

            if (currentNode == null)
            {
                return newNode;
            }

            if (currentNode.Name != newNode.Name)
            {
                string exeptionMessage = String.Format(CultureInfo.InvariantCulture, "Cannot merge the two unrelated nodes with names {0} and {1}", currentNode.Name, newNode.Name);
                throw new XmlException(exeptionMessage);
            }

            if (newNode.Attributes != null)
            {
                foreach (XmlAttribute attribute in newNode.Attributes)
                {
                    currentNode.Attributes.SetNamedItem(attribute);
                }
            }

            if (newNode.ChildNodes != null)
            {
                foreach (XmlNode newChild in newNode.ChildNodes)
                {
                    // Special handing of ARC elements. Assume 'add', 'remove' and 'clear' are reserved
                    if (newChild.Name == "add" || newChild.Name == "remove" || newChild.Name == "clear")
                    {
                        currentNode.AppendChild(newChild.Clone());
                        continue;
                    }

                    bool isFound = false;
                    foreach (XmlNode currentChild in currentNode.ChildNodes)
                    {
                        if (currentChild.Name == newChild.Name)
                        {
                            currentNode.ReplaceChild(XmlMerge(currentChild, newChild).Clone(), currentChild);
                            isFound = true;
                            break;
                        }
                    }
                    if (!isFound)
                    {
                        currentNode.AppendChild(newChild.Clone());
                    }
                }
            }

            return currentNode;
        }

        private static XmlNode XmlAddRemoveClear(XmlNode currentNode, XmlNode newNode)
        {
            if (newNode == null)
            {
                return currentNode;
            }

            if (currentNode == null)
            {
                return newNode;
            }

            if (currentNode.Name != newNode.Name)
            {
                string exeptionMessage = String.Format(CultureInfo.InvariantCulture, "Cannot merge the two unrelated nodes with names {0} and {1}", currentNode.Name, newNode.Name);
                throw new XmlException(exeptionMessage);
            }

            if (newNode.Attributes != null)
            {
                foreach (XmlAttribute attribute in newNode.Attributes)
                {
                    currentNode.Attributes.SetNamedItem(attribute);
                }
            }

            if (newNode.ChildNodes != null)
            {
                foreach (XmlNode newChild in newNode.ChildNodes)
                {
                    if (newChild.Name == "add")
                    {
                        // Support either the 'name' or the 'key' attribute  - but not anything else.
                        string attributeName = newChild.Attributes["name"] != null ? "name" : "key";

                        bool isFound = false;
                        foreach (XmlNode currentChild in currentNode.ChildNodes)
                        {
                            if (currentChild.Name == "add")
                            {
                                if (newChild.Attributes[attributeName].Value == currentChild.Attributes[attributeName].Value)
                                {
                                    currentNode.ReplaceChild(newChild.Clone(), currentChild);
                                    isFound = true;
                                    break;
                                }
                            }
                        }
                        if (isFound)
                        {
                            continue;
                        }
                    }
                    currentNode.AppendChild(newChild.Clone());
                }
            }

            return currentNode;
        }
    }
}