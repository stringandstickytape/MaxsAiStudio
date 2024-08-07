namespace AiTool3.UI
{
    /*                               type: 'applyFindAndReplace',
                                   content: "",
                                   guid: guid,
                                   dataType: dataType,
                                   codeBlockIndex: index.toString()
      */
    public class ChatWebViewAddBranchEventArgs
    {
        public string Type { get; set; }
        public string Content { get; set; }
        public string Guid { get; set; }
        public string DataType { get; set; }
        public int CodeBlockIndex { get; set; }
        public string FindAndReplacesJson { get; set; }
        public string SelectedMessageGuid { get; internal set; }
    }
}