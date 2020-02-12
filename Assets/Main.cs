using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour {
    struct Operator {
        public uint type;
        public int inputCount;
        public string formatString;
    }

    enum NodeType {Operator, Constant, Input}
    class Node {
        public NodeType type;
        public Node parent = null;
        public List<Node> children = new List<Node>();
        public uint data;
        public Operator _operator; 
    }

    class Generation {
        public RenderTexture texture;
        public string text;
        public ComputeBuffer programBuffer = null;
        public int instructionCount;
        public int memoryCellCountRequired;
    }
    
    public ComputeShader cs;
    public Vector3Int threadsPerGroup;
    public Vector3Int threadGroups;
    public Vector3Int textureSize = new Vector3Int(256, 256, 1);
    public Texture2D cpuTexture;
    private bool showCpuVersion = false;
    private bool animate = true;

    private Texture2D cellBackgroundTexture;
    private uint nextOpenMemoryIndex = 0;
    private List<uint> programList = new List<uint>();
    private Generation[] generations;
    private int generationsX = 4;
    private int generationsY = 3;

    const uint XOR = 0;
    const uint ADD = 1;
    const uint MULTIPLY = 2;
    const uint OR = 3;
    const uint AND = 4;
    const uint SUBTRACT = 5;
    const uint CONSTANT = 1000;
    const uint INPUT = 1001;

    public int time = 0;

    void Awake() {
        cellBackgroundTexture = new Texture2D(1, 1);
        cellBackgroundTexture.SetPixel(0, 0, new Color(0, 0, 0, 1));
        cellBackgroundTexture.Apply();

        GetComponent<Camera>().orthographicSize = Screen.height * 0.5f;
        
        generations = new Generation[generationsX * generationsY];
        for (int i = 0; i < generations.Length; i++) {
            generations[i] = new Generation();
            RenderTexture texture = new RenderTexture(textureSize.x, textureSize.y, 0, RenderTextureFormat.ARGB32);
            texture.enableRandomWrite = true;
            texture.Create();
            generations[i].texture = texture;
        }

        cpuTexture = new Texture2D(textureSize.x, textureSize.y, TextureFormat.ARGB32, false);

        threadsPerGroup = new Vector3Int(8, 8, 1);
        threadGroups = new Vector3Int(Mathf.CeilToInt((float)textureSize.x / threadsPerGroup.x), Mathf.CeilToInt((float)textureSize.y / threadsPerGroup.y), Mathf.CeilToInt(1f / threadsPerGroup.z));
    }

    void Update() {
        bool updateTextures = false;

        if (Input.GetMouseButtonDown(1)) {
            updateTextures = true;
            time = 0;
            for (int i = 0; i < generations.Length; i++) {
                Generation generation = generations[i];

                Debug.Log("Generation " + i);

                RenderTexture texture = generation.texture;
                
                Node rootNode = CreateAst(15);
                generation.text = ComposeAstString(rootNode);
                Debug.Log(generation.text);

                {
                    // v2: Instruction: ID, result memory index, value memory indices...
                    // +: 0, r, x, y
                    // input: 1, r, v
                    // constant: 2, r, c
                    programList.Clear();
                    nextOpenMemoryIndex = 0;
                    CompileNode(rootNode);
                    //Debug.Log("Memory cells required: " + nextOpenMemoryIndex);
                    //Debug.Log("Program instruction count: " + programList.Count);
                    
                    string s = "";
                    for (int j = 0; j < programList.Count; j++) {
                        s += programList[j] + ", ";
                    }
                    //Debug.Log("Instructions:" + s);

                    int bytesPerElement = sizeof(uint);
                    if (generation.programBuffer != null) generation.programBuffer.Release();
                    generation.programBuffer = new ComputeBuffer(programList.Count, bytesPerElement, ComputeBufferType.Default);
                    generation.programBuffer.SetData(programList.ToArray());
                }

                if (showCpuVersion) {
                    uint CalculateNode(Node node, uint x, uint y, uint z) {
                        if (node.type == NodeType.Operator) {
                            switch (node._operator.type) {
                                case XOR:
                                    return CalculateNode(node.children[0], x, y, z) ^ CalculateNode(node.children[1], x, y, z);
                                case ADD:
                                    return CalculateNode(node.children[0], x, y, z) + CalculateNode(node.children[1], x, y, z);
                                case MULTIPLY:
                                    return CalculateNode(node.children[0], x, y, z) * CalculateNode(node.children[1], x, y, z);
                                case OR:
                                    return CalculateNode(node.children[0], x, y, z) | CalculateNode(node.children[1], x, y, z);
                                case AND:
                                    return CalculateNode(node.children[0], x, y, z) & CalculateNode(node.children[1], x, y, z);
                                case SUBTRACT:
                                    return CalculateNode(node.children[0], x, y, z) - CalculateNode(node.children[1], x, y, z);
                                default:
                                    Debug.LogError("Unknown operator type");
                                    return 0;
                            }
                        } else if (node.type == NodeType.Constant) {
                            return node.data;
                        } else if (node.type == NodeType.Input) {
                            if (node.data == 0) {
                                return x;
                            } else if (node.data == 1) {
                                return y;
                            } else if (node.data == 2) {
                                return z;
                            } else {
                                Debug.LogError("Unknown input index");
                                return 0;
                            }
                        } else {
                            Debug.LogError("Unknown node type in calculateNode");
                            return 0;
                        }
                    }

                    for (uint y = 0; y < cpuTexture.height; y++) {
                        for (uint x = 0; x < cpuTexture.width; x++) {
                            float[] v = new float[3];
                            for (uint z = 0; z < 3; z++) {
                                v[z] = ((float)(CalculateNode(rootNode, x, y, z) % 256)) / 255f;
                            }
                            cpuTexture.SetPixel((int)x, (int)y, new Color(v[0], v[1], v[2], 1f));
                        }
                    }
                    cpuTexture.Apply();
                }
            }
        }

        if (updateTextures || animate) {
            foreach (Generation generation in generations) {
                if (generation.programBuffer != null) {
                    int kernelIndex = cs.FindKernel("Main");
                    cs.SetTexture(kernelIndex, "Texture", generation.texture);
                    cs.SetBuffer(kernelIndex, "ProgramBuffer", generation.programBuffer);
                    cs.SetInt("ProgramBufferElementCount", generation.programBuffer.count);
                    cs.SetInt("Time", time);
                    cs.Dispatch(kernelIndex, threadGroups.x, threadGroups.y, threadGroups.z);
                }
            }
            Debug.Log("Time: " + time);
        } 
    }

    void FixedUpdate() {
        time++;
    }

    void OnDestroy() {
        foreach (Generation g in generations) {
            g.programBuffer.Release();
        }
    }

    uint CompileNode(Node node) {
        if (node.type == NodeType.Operator) {
            uint[] childMemoryIndices = new uint[node.children.Count];
            for (int i = 0; i < node.children.Count; i++) {
                childMemoryIndices[i] = CompileNode(node.children[i]);
            }
            programList.Add(node._operator.type);
            uint memoryIndex = nextOpenMemoryIndex++;
            programList.Add(memoryIndex);
            for (int i = 0; i < node.children.Count; i++) {
                programList.Add(childMemoryIndices[i]);
            }
            return memoryIndex; 
        } else if (node.type == NodeType.Constant) {
            programList.Add(CONSTANT); // "Set constant" operator id
            uint memoryIndex = nextOpenMemoryIndex++;
            programList.Add(memoryIndex);
            programList.Add(node.data);
            return memoryIndex;
        } else if (node.type == NodeType.Input) {
            programList.Add(INPUT); // "Set input" operator id
            uint memoryIndex = nextOpenMemoryIndex++;
            programList.Add(memoryIndex);
            programList.Add(node.data);
            return memoryIndex;
        } else {
            Debug.LogError("Unknown node type");
            return 0;
        }
    }

    void OnGUI() {
        int gap = 8;
        int i = 0;
        int outline = 2;
        int totalWidth = gap + (textureSize.x + gap) * generationsX;
        int totalHeight = gap + (textureSize.y + gap) * generationsY;
        for (int y = 0; y < generationsY; y++) {
            for (int x = 0; x < generationsX; x++) {
                if (i < generations.Length) {
                    RenderTexture texture = generations[i].texture;
                    int x1 = gap + x * (texture.width + gap) + (Screen.width / 2 - totalWidth / 2);
                    int y1 = gap + y * (texture.height + gap) + (Screen.height / 2 - totalHeight / 2);
                    GUI.skin.box.normal.background = cellBackgroundTexture;
                    GUI.Box(new Rect(x1 - outline, y1 - outline, texture.width + outline * 2, texture.height + outline * 2), GUIContent.none);
                    GUI.DrawTexture(new Rect(x1, y1, texture.width, texture.height), texture);
                }
                i++;
            }
        }
        
        if (showCpuVersion) {
            GUI.DrawTexture(new Rect(264f, 0f, 256f, 256f), cpuTexture);
        }
    }
    
    Node CreateAst(int instructionCount) {
        List<Node> openNodes = new List<Node>();
        List<Operator> operators = new List<Operator>();
        operators.Add(new Operator {type = 0, inputCount = 2, formatString = "({0} ^ {1})"});
        operators.Add(new Operator {type = 1, inputCount = 2, formatString = "({0} + {1})"});
        operators.Add(new Operator {type = 2, inputCount = 2, formatString = "({0} * {1})"});
        operators.Add(new Operator {type = 3, inputCount = 2, formatString = "({0} | {1})"});
        operators.Add(new Operator {type = 4, inputCount = 2, formatString = "({0} & {1})"});
        operators.Add(new Operator {type = 5, inputCount = 2, formatString = "({0} - {1})"});
        int inputCount = 4;

        Node root = null;
        
        // Create operator nodes
        for (int i = 0; i < instructionCount; i++) {
            Operator _operator = operators[Random.Range(0, operators.Count)];
            
            Node newNode = new Node();
            newNode.type = NodeType.Operator;
            newNode._operator = _operator;
            for (int j = 0; j < _operator.inputCount; j++) {
                newNode.children.Add(null);
            }

            if (openNodes.Count == 0) {
                root = newNode;
            } else {
                Node parent = openNodes[Random.Range(0, openNodes.Count)];

                List<int> openChildIndices = new List<int>();
                for (int j = 0; j < parent.children.Count; j++) {
                    if (parent.children[j] == null) {
                        openChildIndices.Add(j);
                    }
                }
                int childIndex = openChildIndices[Random.Range(0, openChildIndices.Count)];
                parent.children[childIndex] = newNode;
                newNode.parent = parent;

                if (openChildIndices.Count == 1) {
                    openNodes.Remove(parent);
                }
            }

            openNodes.Add(newNode);
        }

        // Create leaf nodes
        foreach (Node parent in openNodes) {
            for (int i = 0; i < parent.children.Count; i++) {
                if (parent.children[i] == null) {
                    NodeType childNodeType = Random.value < 0.5 ? NodeType.Constant : NodeType.Input;
                    if (childNodeType == NodeType.Constant) {
                        Node newNode = new Node();
                        newNode.type = NodeType.Constant;
                        newNode.data = (uint)((long)Random.Range(int.MinValue, int.MaxValue) - (long)int.MinValue);
                        parent.children[i] = newNode;
                        newNode.parent = parent;
                    } else if (childNodeType == NodeType.Input) {
                        Node newNode = new Node();
                        newNode.type = NodeType.Input;
                        newNode.data = (uint)Random.Range(0, inputCount);
                        parent.children[i] = newNode;
                        newNode.parent = parent;
                    }
                }
            }
        }

        return root;
    }

    string ComposeAstString(Node node) {
        if (node.type == NodeType.Operator) {
            Operator _operator = node._operator;
            
            object[] childTexts = new object[node.children.Count];
            for (int i = 0; i < node.children.Count; i++) {
                childTexts[i] = ComposeAstString(node.children[i]);
            }

            return string.Format(_operator.formatString, childTexts);
        } else if (node.type == NodeType.Constant) {
            return node.data.ToString();
        } else if (node.type == NodeType.Input) {
            return "In" + node.data;
        } else {
            return "N/A";
        }
    }
}
