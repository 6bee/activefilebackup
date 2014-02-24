// Copyright (c) Christof Senn. All rights reserved. See license.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Configuration;

namespace ActiveFileBackup.Configuration
{
    public class Folders : ConfigurationElementCollection, IEnumerable<Folder>
    {
        public Folders()
        {
        }

        public override ConfigurationElementCollectionType CollectionType
        {
            get { return ConfigurationElementCollectionType.AddRemoveClearMap; }
        }

        protected override ConfigurationElement CreateNewElement()
        {
            return new Folder();
        }

        protected override ConfigurationElement CreateNewElement(string path)
        {
            return new Folder(path);
        }


        protected override Object GetElementKey(ConfigurationElement element)
        {
            return ((Folder)element).Path;
        }

        public new string AddElementName
        {
            get { return base.AddElementName; }
            set { base.AddElementName = value; }
        }

        public new string ClearElementName
        {
            get { return base.ClearElementName; }
            set { base.AddElementName = value; }
        }

        public new string RemoveElementName
        {
            get { return base.RemoveElementName; }
        }

        public new int Count
        {
            get { return base.Count; }
        }

        public Folder this[int index]
        {
            get { return (Folder)BaseGet(index); }
            set
            {
                if (BaseGet(index) != null)
                {
                    BaseRemoveAt(index);
                }
                BaseAdd(index, value);
            }
        }

        new public Folder this[string path]
        {
            get { return (Folder)BaseGet(path); }
        }

        public int IndexOf(Folder folder)
        {
            return BaseIndexOf(folder);
        }

        public void Add(Folder folder)
        {
            BaseAdd(folder, false);
        }

        protected override void BaseAdd(ConfigurationElement element)
        {
            BaseAdd(element, false);
        }

        public void Remove(Folder folder)
        {
            if (BaseIndexOf(folder) >= 0) BaseRemove(folder.Path);
        }

        public void RemoveAt(int index)
        {
            BaseRemoveAt(index);
        }

        public void Remove(string name)
        {
            BaseRemove(name);
        }

        public void Clear()
        {
            BaseClear();
        }

        public new IEnumerator<Folder> GetEnumerator()
        {
            var count = Count;
            for (int i = 0; i < count; i++)
                yield return this[i];
        }
    }
}
