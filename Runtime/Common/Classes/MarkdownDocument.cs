namespace Metamatic {
    using System.Collections.Generic;
    
    public class MarkdownDocument {
        public class Node {
            public List<Node> children {
                get; private set;
            } = new List<Node>();

            public Node(Node[] children = null) {
                if (children != null) {
                    for (int i = 0; i < children.Length; i++) {
                        this.children.Add(children[i]);
                    }
                }
            }
        }
        public class RootNode: Node {

        }
        public class Pharagraph: Node {
            public Pharagraph(Node[] children): base(children) {

            }
        }
        public class TextNode: Node {
            public string value;

            public TextNode(string value = ""): base() {
                this.value = value;
            }
        }
        public class InlineCodeNode: TextNode {
            public InlineCodeNode(string value): base(value) {}
        }
        public class LinkNode: Node {
            public string url = "";
            public LinkNode(string url, Node[] children): base(children) {
                this.url = url;
            }
        }
        public class ImageNode: Node {
            public string url = "";
            public string alt = "";
            public ImageNode(string url, string alt = ""): base() {
                this.url = url;
                this.alt = alt;
            }
        }
        public class CodeNode: Node {
            public string lang;
            public string value = "";
            public CodeNode(string value, string lang = null): base() {
                this.value = value;
                this.lang = lang;
            }
        }
        public class EmphasisNode: Node {
            public EmphasisNode(Node[] children): base(children) {

            }
        }
        public class StrongNode: Node {
            public StrongNode(Node[] children): base(children) {
                
            }
        }
        public class HeadingNode: Node {
            public int depth = 1;

            public HeadingNode(Node[] children, int depth = 1): base() {
                this.depth = depth;

                if (children == null) {
                    throw new System.Exception("Heading must have children");
                }

                for (int i = 0; i < children.Length; i++) {
                    this.children.Add(children[i]);
                }
            }
        }
        public class Receiver {
            public delegate string Handler(string memo, string line);
            Handler handler;
            public string memo = "";
            public Receiver(Handler handler) {
                this.handler = handler;
            }

            public void Add(string value) {
                memo = this.handler(memo, value);
            }
        }
        public readonly RootNode root;
        public MarkdownDocument(string sourceText) {
            root = new RootNode();
            string[] lines = sourceText.Replace("\r", "").Split(new [] { '\n' });
            List<Node> pharagrapNodes = new List<Node>();
            Receiver activeReceiver = null;

            for (int i = 0; i < lines.Length; i++) {
                if (activeReceiver != null) {
                    activeReceiver.Add(lines[i]);
                } else if (lines[i].Length == 0) {
                    if (pharagrapNodes.Count > 0) {
                        root.children.Add(new Pharagraph(pharagrapNodes.ToArray()));
                        pharagrapNodes.Clear();
                    }
                /* Test for heading */
                } else if (lines[i].Substring(0, 1) == "#") {
                    /* Looks like heading 1 */
                    if (lines[i].Substring(0, 2) == "##") {
                        /* Looks like heading 2 */

                        if (lines[i].Substring(0, 3) == "###") {
                            /* Looks like heading 3 */

                            if (lines[i].Substring(0, 4) == "####") {
                                /* Looks like heading 4 */

                                if (lines[i].Substring(0, 5) == "#####") {
                                    root.children.Add(new HeadingNode(MarkdownDocument.ParseLine(lines[i].Substring(6)), 5));
                                } else {
                                    root.children.Add(new HeadingNode(MarkdownDocument.ParseLine(lines[i].Substring(5)), 4));
                                }
                            } else {
                                root.children.Add(new HeadingNode(MarkdownDocument.ParseLine(lines[i].Substring(4)), 3));
                            }
                        } else {
                            root.children.Add(new HeadingNode(MarkdownDocument.ParseLine(lines[i].Substring(3)), 2));
                        }
                    } else {
                        root.children.Add(new HeadingNode(MarkdownDocument.ParseLine(lines[i].Substring(2)), 1));
                    }
                /* Test for code block */
                } else if (lines[i].Substring(0, 3) == "```") {
                    var lang = lines[i].Substring(3);
                    activeReceiver = new Receiver((string memo, string line) => {
                        if (line.Substring(0, 3) == "```") {
                            root.children.Add(new CodeNode(memo, lang));
                            activeReceiver = null;
                            return null;
                        }
                        return memo.Length == 0 ? line : $"{memo}\n{line}";
                    });
                } else {
                    var lineNodes = MarkdownDocument.ParseLine(lines[i]);
                    if (pharagrapNodes.Count > 0) {
                        if (lineNodes[0].GetType() == typeof(TextNode)) {
                            var firstTextNode = ((TextNode)lineNodes[0]);
                            firstTextNode.value = $"\n{firstTextNode.value}";
                        } else {
                            pharagrapNodes.Add(new TextNode("\n"));
                        }
                    }
                    
                    for (int q = 0; q < lineNodes.Length; q++) {
                        pharagrapNodes.Add(lineNodes[q]);
                    }
                }
            }

            root.children.Add(new Pharagraph(pharagrapNodes.ToArray()));
        }

        public static Node[] ParseLine(string line) {
            List<Node> nodes = new List<Node>();
            string text = "";
            for(int i = 0; i < line.Length; i++) {
                /* Looks for inline block */
                if (line.Substring(i, 1) == "`") {
                    /* Search for closing quote */
                    var closingIndex = line.IndexOf("`", i + 1);
                    if (closingIndex == -1) {
                        text += line.Substring(i, 1);
                        continue;
                    } else {
                        if (text.Length > 0) {
                             nodes.Add(new TextNode(text));
                             text = "";
                        }
                        nodes.Add(new InlineCodeNode(line.Substring(i + 1, closingIndex - i - 1)));
                        i = closingIndex;
                    }
                /* Looks for link */
                } else if (line.Substring(i, 1) == "[") {
                    var closingIndex = line.IndexOf("]", i + 1);
                    if (closingIndex == -1 || line.Substring(closingIndex + 1, 1) != "(") {
                        text += line.Substring(i, 1);
                        continue;
                    } else {
                        var lastClosingIndex = line.IndexOf(")", closingIndex + 1);
                        if (lastClosingIndex == -1) {
                            text += line.Substring(i, 1);
                            continue;
                        } else {
                            if (text.Length > 0) {
                                nodes.Add(new TextNode(text));
                                text = "";
                            }
                            nodes.Add(new LinkNode(line.Substring(closingIndex + 2, lastClosingIndex - closingIndex - 2), ParseLine(line.Substring(i + 1, closingIndex - 1 - i))));
                            i = lastClosingIndex;
                        }
                    }
                } else if (line.Length > i + 3 && line.Substring(i, 2) == "![") {
                    var closingIndex = line.IndexOf("]", i + 2);
                    if (closingIndex == -1 || line.Substring(closingIndex + 1, 1) != "(") {
                        text += line.Substring(i, 1);
                        continue;
                    } else {
                        var lastClosingIndex = line.IndexOf(")", closingIndex + 1);
                        if (lastClosingIndex == -1) {
                            text += line.Substring(i, 1);
                            continue;
                        } else {
                            if (text.Length > 0) {
                                nodes.Add(new TextNode(text));
                                text = "";
                            }
                            nodes.Add(new ImageNode(line.Substring(closingIndex + 2, lastClosingIndex - closingIndex - 2), line.Substring(i + 2, closingIndex - (i + 2))));
                            i = lastClosingIndex;
                        }
                    }
                /* Looks for italic, bold string */
                } else if (line.Substring(i, 1) == "*") {
                    byte mode = 0; // emphasis
                    if (line.Substring(i, 2) == "**") {
                        mode = 1; // bold
                    }

                    var closingIndex = line.IndexOf("*", i + (mode == 1 ? 2 : 1));

                    if (closingIndex == -1 || (mode == 1 && line.Substring(closingIndex + 1, 1) != "*")) {
                        text += line.Substring(i, 1);
                        continue;
                    } else {
                        if (text.Length > 0) {
                            nodes.Add(new TextNode(text));
                            text = "";
                        }
                        if (mode == 0) {
                            nodes.Add(new EmphasisNode(ParseLine(line.Substring(i + 1, closingIndex - 1 - i))));
                            i = closingIndex;
                        } else {
                            nodes.Add(new StrongNode(ParseLine(line.Substring(i + 2, closingIndex - (i + 2)))));
                            i = closingIndex + 1;
                        }
                    }
                } else {
                    text += line.Substring(i, 1);
                }
            }

            if (text.Length > 0) {
                nodes.Add(new TextNode(text));
            }

            return nodes.ToArray();
        }
    }
}