
namespace CORE.Engines
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Xml;
    using System.Text;
    using System.Text.RegularExpressions;

    [Serializable]
    public class SequenceBranchPipingEngine : IEngine
    {


        public SequenceBranchPipingEngine() { }

        private int _oldpos;
        private int _pos;
        private int _pushstack;
        private string _msg = string.Empty;
        private string _parameters = string.Empty;

        private ArrayList _store;
        private Stack _backwardStack;
        private string _form;

        private Hashtable _targets;


        private Hashtable _criterias; 	//[FormItemOID, Array(criteria)]
        private Hashtable _criteriasMeta; 	//[position, Array(criteria)]


        private Hashtable piping = new Hashtable();
        private Hashtable pipingMeta = new Hashtable();


        private string _direction = string.Empty;

        private string test = string.Empty;


        private void init()
        {

            _store = new ArrayList();
            _backwardStack = new Stack();
            _backwardStack.Push(0);
            _pushstack = -1;
            _oldpos = -1;
            _pos = -1; // position within the nodelist
            _finished = false;

            _criterias = new Hashtable();
            _criteriasMeta = new Hashtable();

            _targets = new Hashtable();

        }

        public void rollbackPosition()
        {
            _pos = _oldpos;
        }


        private void loadPiping(XmlDocument doc)
        {
            //tw.WriteLine( "load piping:" );
            XmlNodeList items = doc.GetElementsByTagName("Item");
            XmlNodeList resources = doc.DocumentElement.SelectNodes("descendant::Resource");

            for (int i = 0; i < items.Count; i++)
            {
                string ID = items[i].Attributes["ID"].Value;
                string Response = items[i].Attributes["Response"].Value;

                Regex rgx = new Regex(@"<" + ID + ">.*</>");

                for (int j = 0; j < resources.Count; j++)
                {
                    if (resources[j].Attributes["Description"] != null && rgx.IsMatch(resources[j].Attributes["Description"].Value))
                    {

                        p_results p = new p_results(resources[j].Attributes["ResourceOID"].Value, Response);

                        if (!piping.Contains(ID))
                        {
                            ArrayList a = new ArrayList();
                            a.Add(p);
                            piping[ID] = a;
                            //tw.WriteLine( "New pipe:" + ID + ": ResourceOID:" + p.ID);
                        }
                        else
                        {
                            ArrayList a = (ArrayList)piping[ID];
                            a.Add(p);
                            piping[ID] = a;
                            //tw.WriteLine( "Adding pipe:" + ID + ": ResourceOID:" + p.ID);
                        }


                        // Linking resourceOID to all pipes
                        if (!pipingMeta.Contains(resources[j].Attributes["ResourceOID"].Value))
                        {
                            ArrayList a = new ArrayList();
                            a.Add(ID);
                            pipingMeta[resources[j].Attributes["ResourceOID"].Value] = a;
                            //tw.WriteLine( "New meta pipe:" + ID + ": ResourceOID:" + resources[j].Attributes["ResourceOID"].Value);
                        }
                        else
                        {
                            ArrayList a = (ArrayList)pipingMeta[resources[j].Attributes["ResourceOID"].Value];
                            a.Add(ID);
                            pipingMeta[resources[j].Attributes["ResourceOID"].Value] = a;
                            //tw.WriteLine( "New meta pipe:" + ID + ": ResourceOID:" + resources[j].Attributes["ResourceOID"].Value);
                        }
                    }
                }

            }
        }

        private void loadParamPiping(XmlDocument doc)
        {
            //tw.WriteLine( "loadParamPiping:" );
            XmlNodeList items = doc.DocumentElement.SelectNodes("IDS/ID"); // FormParams.GetElementsByTagName("ID");
            XmlNodeList resources = doc.DocumentElement.SelectNodes("descendant::Resource");

            //tw.WriteLine( "loadParamPiping:" + items.Count.ToString()); 
            for (int i = 0; i < items.Count; i++)
            {
                string ID = items[i].Attributes["VariableName"].Value;
                string Response = items[i].Attributes["Response"].Value;

                Regex rgx = new Regex(@"<" + ID + ">.*</>");

                for (int j = 0; j < resources.Count; j++)
                {
                    if (resources[j].Attributes["Description"] != null && rgx.IsMatch(resources[j].Attributes["Description"].Value))
                    {

                        p_results p = new p_results(resources[j].Attributes["ResourceOID"].Value, Response);

                        if (!piping.Contains(ID))
                        {
                            ArrayList a = new ArrayList();
                            a.Add(p);
                            piping[ID] = a;
                            //tw.WriteLine( "New pipe:" + ID + ": ResourceOID:" + p.ID);
                        }
                        else
                        {
                            ArrayList a = (ArrayList)piping[ID];
                            a.Add(p);
                            piping[ID] = a;
                            //tw.WriteLine( "Adding pipe:" + ID + ": ResourceOID:" + p.ID);
                        }


                        // Linking resourceOID to all pipes
                        if (!pipingMeta.Contains(resources[j].Attributes["ResourceOID"].Value))
                        {
                            ArrayList a = new ArrayList();
                            a.Add(ID);
                            pipingMeta[resources[j].Attributes["ResourceOID"].Value] = a;
                            //tw.WriteLine( "New meta pipe:" + ID + ": ResourceOID:" + resources[j].Attributes["ResourceOID"].Value);
                        }
                        else
                        {
                            ArrayList a = (ArrayList)pipingMeta[resources[j].Attributes["ResourceOID"].Value];
                            a.Add(ID);
                            pipingMeta[resources[j].Attributes["ResourceOID"].Value] = a;
                            //tw.WriteLine( "New meta pipe:" + ID + ": ResourceOID:" + resources[j].Attributes["ResourceOID"].Value);
                        }
                    }
                }

            }
            //tw.Close();
        }

        private void setPipingValue(XmlNode node)
        {

            string key = node.Attributes["ID"].Value;
            ArrayList a = (ArrayList)piping[key];

            for (int i = 0; i < a.Count; i++)
            {
                p_results c = (p_results)a[i];
                c.value = node.Attributes["Response"].Value;
                a[i] = c;
            }
            piping[key] = a;
        }

        private void replacePipedValue(XmlNode node)
        {

            try
            {

                XmlNodeList resources = node.SelectNodes("descendant::Resource");

                for (int j = 0; j < resources.Count; j++)
                {

                    if ((resources[j].Attributes["ResourceOID"] != null) && pipingMeta.Contains(resources[j].Attributes["ResourceOID"].Value))
                    {

                        //tw.WriteLine("found resource " + resources[j].Attributes["ResourceOID"].Value + " to process ");

                        ArrayList a = (ArrayList)pipingMeta[resources[j].Attributes["ResourceOID"].Value];
                        for (int i = 0; i < a.Count; i++)
                        {

                            string key = (string)a[i];

                            //tw.WriteLine("There are " + a.Count.ToString() + " resources that are being replaced, which one should we choose now?" );

                            ArrayList b = (ArrayList)piping[key];
                            for (int k = 0; k < b.Count; k++)
                            {

                                p_results p = (p_results)b[k];

                                if (p.ID == resources[j].Attributes["ResourceOID"].Value)
                                {
                                    //tw.WriteLine( "Before:ReplacePipingValue:" + key + ": Value:" + p.value + " : in :" + resources[j].Attributes["ResourceOID"].Value + ":" + resources[j].Attributes["Description"].Value);
                                    resources[j].Attributes["Description"].Value = Regex.Replace(resources[j].Attributes["Description"].Value, "<" + key + ">.*?</>", "<" + key + ">" + p.value + "</>");

                                    //tw.WriteLine( "After:ReplacePipingValue:" + key + ": Value:" + p.value + " : in :" + resources[j].Attributes["ResourceOID"].Value  + ":" + resources[j].Attributes["Description"].Value);
                                }
                            }

                        }

                    }


                }
            }
            catch (Exception)
            {
                //tw.WriteLine(ex.Message);
                //tw.Close();
            }

        }

        private XmlNode loadValidation()
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlDocument xmlVal = new XmlDocument();
            XmlElement root = xmlVal.CreateElement("Validation");
            XmlNode Validation = xmlVal.GetElementsByTagName("Validation")[0];
            foreach (String item in _store)
            {
                XmlDocument itemDoc = new XmlDocument();
                StringReader myStream = new StringReader(item);
                XmlTextReader txtReader = new XmlTextReader(myStream);
                itemDoc.Load(txtReader);
                XmlNodeList critera = itemDoc.GetElementsByTagName("Item");
                for (int i = 0; i < critera.Count; i++)
                {
                    XmlElement Validate = xmlVal.CreateElement("Validate");
                    Validate.SetAttribute("ID", critera[i].Attributes["ID"].Value);
                    Validate.SetAttribute("Response", critera[i].Attributes["Response"].Value);
                    root.AppendChild(Validate);
                }    
            }
            xmlVal.AppendChild(root);
            return Validation;
        }


        private void loadCriterias(XmlDocument doc)
        {

            XmlNodeList crit = doc.GetElementsByTagName("Criteria");

            for (int i = 0; i < crit.Count; i++)
            {
                criteria c = new criteria(
                       crit[i].Attributes["Target_FormItemOID"].Value,
                       crit[i].Attributes["Criteria_FormItemOID"].Value,
                       crit[i].Attributes["Criteria_ItemResponseOID"].Value,
                       Int32.Parse(crit[i].Attributes["Operator"].Value),
                       Int32.Parse(crit[i].Attributes["DefaultResult"].Value),
                       string.Empty);



                if (!_criterias.Contains(c.FormItemOID))
                {
                    ArrayList a = new ArrayList();
                    a.Add(c);
                    _criterias[c.FormItemOID] = a;
                }
                else
                {
                    ArrayList a = (ArrayList)_criterias[c.FormItemOID];
                    a.Add(c);
                    _criterias[c.FormItemOID] = a;
                }

            }


        }

        public bool loadItems(XmlDocument doc, XmlDocument ItemparamsDoc, bool WithHeader)
        {

            bool rtn = false;
            int Answered = 0;

            if (_store == null)
            {
                this.init();

                /* Load Items as strings in collection */

                loadPiping(doc);
                loadParamPiping(doc);

                doc.GetElementsByTagName("Form")[0].Attributes["Engine"].Value = "SequenceBranchPipingEngine";

                loadCriterias(ItemparamsDoc);

                XmlNodeList items = doc.GetElementsByTagName("Item");

                for (int i = 0; i < items.Count; i++)
                {
                    StringWriter sw = new StringWriter();
                    XmlTextWriter xw = new XmlTextWriter(sw);

                    if (items[i].Attributes["ID"].Value == string.Empty && !WithHeader)
                    {
                        continue;
                    }

                    if (items[i].Attributes["Response"].Value != String.Empty)
                    {
                        Answered += 1;
                        _IsResume = true;
                    }


                    // *************************************** //
                    //XmlNodeList itemParams = ItemparamsDoc.GetElementsByTagName("Criterias")[0].SelectNodes("descendant::Criteria[@Criteria_FormItemOID ='" + items[i].Attributes["FormItemOID"].Value + "']");


                    //if(itemParams != null && itemParams.Count > 0){

                    //	linkCriterias(itemParams);
                    //}
                    // ****************************************** //


                    // *************************************** //
                    /* Cannot do this here, targets must be populated.*/
                    //if(_criterias.Contains(items[i].Attributes["FormItemOID"].Value)){
                    //    setCriteriaValue(items[i]);
                    //}

                    // ****************************************** //

                    items[i].Attributes["Position"].Value = (i + 1).ToString();
                    items[i].WriteTo(xw);


                    _store.Add(sw.ToString());


                    _targets[items[i].Attributes["FormItemOID"].Value.ToUpper()] = (_store.Count - 1);

                }
                // have to reiterate after _targets is populated.
                for (int i = 0; i < items.Count; i++)
                    if (_criterias.Contains(items[i].Attributes["FormItemOID"].Value))
                        setCriteriaValue(items[i]);


                /* Load Items in collection */

                if (Answered == _store.Count)
                {
                    _IsCompleted = true;
                }

                /*   Store Document Shell */
                doc.GetElementsByTagName("Items")[0].RemoveAll();
                StringWriter sw2 = new StringWriter();
                XmlTextWriter xw2 = new XmlTextWriter(sw2);
                doc.WriteTo(xw2);
                _form = sw2.ToString();
                /*   Store Document Shell */

                rtn = true;
            }

            return rtn;
        }

        public string paramPROC
        {
            get { return "dbo.loadItemBranching"; }
        }

        private bool _finished = false;

        public bool finished
        {
            get { return _finished; }
            set { _finished = value; }
        }


        private bool _IsResume = false;

        public bool IsResume
        {
            get { return _IsResume; }
        }

        private bool _IsCompleted = false;

        public bool IsCompleted
        {
            get { return _IsCompleted; }
        }


        public int currentPosition
        {
            get { return _pos; }
            set { _pos = value; }
        }
        public int previousPosition
        {
            get { return _oldpos; }
            set { _oldpos = value; }
        }

        public int TotalItems
        {
            get { return _store.Count; }
        }
        public string message
        {
            get { return _msg; }
            set { _msg = value; }

        }

        public XmlDocument getForm()
        {

            XmlDocument doc = new XmlDocument();
            doc.LoadXml(_form);

            // ***************** RELOAD THE DOCUMENT ********************** //
            XmlDocumentFragment docFrag = doc.CreateDocumentFragment();
            for (int i = 0; i < _store.Count; i++)
            {
                docFrag.InnerXml = (String)_store[i];
                XmlNode deep = docFrag.CloneNode(true);
                doc.GetElementsByTagName("Items")[0].AppendChild(doc.ImportNode(deep, true));
            }

            return doc;

        }

        public XmlDocument getCurrentItem()
        {

            XmlDocument doc = new XmlDocument();
            XmlDocumentFragment docFrag;
            if (!_finished)
            {
                doc.LoadXml(_form);


                if (_pos < _store.Count && _pos > -1)
                {
                    // ***************** RELOAD THE DOCUMENT ********************** //
                    docFrag = doc.CreateDocumentFragment();
                    docFrag.InnerXml = (String)_store[_pos];
                    XmlNode deep = docFrag.CloneNode(true);
                    replacePipedValue(deep);
                    doc.GetElementsByTagName("Items")[0].AppendChild(doc.ImportNode(deep, true));

                    //Update Validations node with previously responded values
                    //<Validation ValidationOID="275AFEBD-FD2E-4D86-A5A4-8DFD8C6EF6A9" Message="" Display="FALSE" Type="" Prompt="VAARBA5_v2" Value="" Min="" Max="" />

                    XmlNodeList vnode = doc.SelectNodes("//Form/Items/Item/Validations/Validation");
                    if (vnode != null)
                    {
                        foreach (XmlNode item in vnode)
                        {
                            string itemName = item.Attributes["Value"].Value;
                            string ValidationOID = item.Attributes["ValidationOID"].Value;
                            XmlNode docNode = doc.SelectSingleNode("//Form/Items/Item/Validations/Validation[@ValidationOID='"+ValidationOID+"']");
                            if (docNode != null)
                            {
                                if (docNode.Attributes.GetNamedItem("Response") != null){
                                    XmlAttribute ResponseAttr = doc.CreateAttribute("Response");
                                    foreach (String update in _store)
                                    {
                                        XmlDocument itemDoc = new XmlDocument();
                                        StringReader myStream = new StringReader(update);
                                        XmlTextReader txtReader = new XmlTextReader(myStream);
                                        itemDoc.Load(txtReader);
                                        XmlNodeList critera = itemDoc.GetElementsByTagName("Item");
                                        for (int i = 0; i < critera.Count; i++)
                                        {
                                            if (critera[i].Attributes["ID"].Value == itemName)
                                            {
                                                docNode.Attributes["Response"].Value = critera[i].Attributes["Response"].Value;
                                            }
                                        }
                                    }
                                } else {

                                    XmlAttribute ResponseAttr = doc.CreateAttribute("Response");
                                    foreach (String update in _store)
                                    {
                                        XmlDocument itemDoc = new XmlDocument();
                                        StringReader myStream = new StringReader(update);
                                        XmlTextReader txtReader = new XmlTextReader(myStream);
                                        itemDoc.Load(txtReader);
                                        XmlNodeList critera = itemDoc.GetElementsByTagName("Item");
                                        for (int i = 0; i < critera.Count; i++)
                                        {
                                            if (critera[i].Attributes["ID"].Value == itemName)
                                            {
                                                ResponseAttr.Value = critera[i].Attributes["Response"].Value;
                                            }
                                        }
                                    }
                                    docNode.Attributes.Append(ResponseAttr);

                                }
                            }
                        }
                    }

                    //_msg = test; //string.Empty; // "[InternalBranchEngine2]Item " + (_pos + 1).ToString() + " of " + _store.Count.ToString(); // + ":: [InternalBranchEngine][" + test + "]";
                }
             
                else
                {
                    // ***************** RELOAD THE DOCUMENT ********************** //
                    docFrag = doc.CreateDocumentFragment();
                    docFrag.InnerXml = (String)_store[_pos - 1];
                    XmlNode deep = docFrag.CloneNode(true);
                    replacePipedValue(deep);
                    doc.GetElementsByTagName("Items")[0].AppendChild(doc.ImportNode(deep, true));

                    _msg = string.Empty; //"[InternalBranchEngine2]Item " + (_pos).ToString() + " of " + _store.Count.ToString(); // + ":: InternalBranchEngine]";

                    _finished = true;
                    return doc;

                }


            }
            else
            {
                doc = null;
            }
           // doc.GetElementsByTagName("Items")[0].AppendChild(doc.ImportNode(loadValidation(), true));
       
            return doc;

        }

        public XmlDocument getPreviousItem()
        {

            _direction = "prev";
            decrementPosition();
            return getCurrentItem();

        }
        public XmlDocument getNextItem()
        {
            if (_pushstack > 0) { _backwardStack.Push(_pushstack); }
            _direction = "next";
            incrementPosition();
            return getCurrentItem();
        }

        private void linkCriterias(XmlNodeList itemParams)
        {

            string key = itemParams[0].Attributes["Criteria_FormItemOID"].Value;

            if (_criterias.Contains(key))
            {
                _criteriasMeta.Add(_store.Count, (ArrayList)_criterias[key]);
            }
            return;

        }


        private string getResponseKey(XmlNode node, string ItemResponseOID)
        {


            string rtn = node.Attributes["ItemResponseOID"].Value.ToUpper();

            if (rtn != Guid.Empty.ToString())
            {
                return rtn;
            }


            XmlNode n = node.SelectSingleNode("descendant::Map[@ItemResponseOID ='" + ItemResponseOID + "']");

            if (n != null)
            {
                try
                {

                    if ((Convert.ToInt32(node.Attributes["Response"].Value) & Convert.ToInt32(n.Attributes["Description"].Value)) == Convert.ToInt32(n.Attributes["Description"].Value))
                    {
                        rtn = ItemResponseOID;
                    }

                }
                catch (Exception ex) { _msg = ex.Message; }
            }

            return rtn;

        }

        private void setCriteriaValue(XmlNode node)
        {


            string key = node.Attributes["FormItemOID"].Value;
            ArrayList a = (ArrayList)_criterias[key];

            for (int i = 0; i < a.Count; i++)
            {
                criteria c = (criteria)a[i];

                string responseKey = getResponseKey(node, c.ItemResponseOID.ToUpper());

                c.value = responseKey;

                if (c.ItemResponseOID == responseKey)
                {
                    // *** GOTO END OF FORM IF Guid is empty;
                    if (c.OID.ToUpper() == Guid.Empty.ToString().ToUpper())
                    {
                        _pos = _store.Count;
                    }
                    else
                    {
                        _pos = (int)_targets[c.OID] - 1;
                    }
                }

                a[i] = c;

            }
            _criterias[key] = a;

        }


        public void updateNode(XmlNode node)
        {
            _pushstack = _pos;
            StringWriter sw = new StringWriter();
            XmlTextWriter xw = new XmlTextWriter(sw);
            node.WriteTo(xw);
            string upstr = sw.ToString();

            if (piping.Contains(node.Attributes["ID"].Value))
            {
                setPipingValue(node);
            }

            if (_criterias.Contains(node.Attributes["FormItemOID"].Value))
            {
                setCriteriaValue(node);
            }

            for (int i = 0; i < _store.Count; i++)
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(_form);
                XmlDocumentFragment docFrag = doc.CreateDocumentFragment();
                docFrag.InnerXml = (String)_store[i];
                XmlNode deep = docFrag.CloneNode(true);
                doc.GetElementsByTagName("Items")[0].AppendChild(doc.ImportNode(deep, true));

                XmlNodeList list = doc.GetElementsByTagName("Items")[0].SelectNodes("descendant::Item[@FormItemOID ='" + node.Attributes["FormItemOID"].Value + "']");

                if (list.Count == 1)
                {
                    _store[i] = upstr;
                    break;
                }
            }
        }


        public void decrementPosition()
        {

            if (_pos == -1)
            {
                _oldpos = _pos;
                _pos = 0;

            }
            if (_pos > 0)
            {
                _oldpos = _pos;
                if ((int)_backwardStack.Peek() == 0)
                {
                    _pos = (int)_backwardStack.Peek();
                    _pushstack = (int)_backwardStack.Peek();
                }
                else
                {
                    _pos = (int)_backwardStack.Pop();
                }
            }

        }
        public void incrementPosition()
        {
            if (_pos == _store.Count)
            {
                _finished = true;
            }
            if (_pos < _store.Count)
            {
                _oldpos = _pos;
                _pos += 1;
            }
        }

        [Serializable]
        private struct criteria
        {

            public string OID;
            public string FormItemOID;
            public string ItemResponseOID;
            public int op;
            public int DefaultResult;
            public string value;

            public criteria(
                string _OID,
                string _FormItemOID,
                string _ItemResponseOID,
                int _op,
                int _DefaultResult,
                string _value
            )
            {
                OID = _OID;
                FormItemOID = _FormItemOID;
                ItemResponseOID = _ItemResponseOID;
                op = _op;
                DefaultResult = _DefaultResult;
                value = _value;


            }
        }

        private struct p_results
        {
            public string ID;
            public string value;

            public p_results(
                string _ID,
                string _value
            )
            {
                ID = _ID;
                value = _value;
            }
        }

    } // END Concrete Class





}  // END NAME SPACE