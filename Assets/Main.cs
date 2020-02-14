using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Main : MonoBehaviour {
    struct Operator {
        public uint type;
        public int inputCount;
        public string formatString;
    }

    struct InputValue {
        public uint type;
        public string formatString;
    }

    enum NodeType {Operator, Constant, Input}
    class Node {
        public NodeType type;
        public Node parent = null;
        public List<Node> children = new List<Node>();
        public uint data;
        public Operator _operator;
        public InputValue inputValue;
    }

    class Generation {
        public RenderTexture texture;
        public string text;
        public ComputeBuffer programBuffer = null;
    }
    
    public ComputeShader cs;
    public Vector3Int threadsPerGroup;
    public Vector3Int threadGroups;
    public Vector3Int textureSize = new Vector3Int(256, 256, 1);
    public Texture2D cpuTexture;
    public Texture2D inputTexture;
    private bool showCpuVersion = false;
    private bool animate = true;
    private bool videoMode = true;

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
    const uint SINE = 6;
    const uint TAN = 7;
    const uint SQRT = 8;
    const uint ANGLE_DISTORT = 9;
    const uint LENGTH_DISTORT = 10;
    const uint INPUT_TEXTURE_DISTORT = 11;

    const uint CONSTANT = 1000;
    const uint INPUT_X = 1001;
    const uint INPUT_Y = 1002;
    const uint INPUT_CHANNEL = 1003;
    const uint INPUT_TIME = 1004;
    const uint INPUT_ANGLE = 1005;
    const uint INPUT_LENGTH = 1006;
    const uint INPUT_TEXTURE_RGB = 1007;

    public int time = 0;

    private List<Operator> operators = new List<Operator>();
    private List<InputValue> inputValues = new List<InputValue>();

    Node CreateAst(int instructionCount) {
        List<Node> openNodes = new List<Node>();

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
                        InputValue inputValue = inputValues[Random.Range(0, inputValues.Count)];
                        newNode.inputValue = inputValue;
                        newNode.data = inputValue.type;
                        parent.children[i] = newNode;
                        newNode.parent = parent;
                    }
                }
            }
        }

        return root;
    }

    void Awake() {
        if (videoMode) {
            generationsX = 7;
            generationsY = 4;
        }
        
        {
            // Cool circular mixed with xy, lots of variation and curves
            if (false) {
                operators.Add(new Operator {type = XOR, inputCount = 2, formatString = "({0} ^ {1})"});
                operators.Add(new Operator {type = ADD, inputCount = 2, formatString = "({0} + {1})"});
                operators.Add(new Operator {type = MULTIPLY, inputCount = 2, formatString = "({0} * {1})"});
                operators.Add(new Operator {type = OR, inputCount = 2, formatString = "({0} | {1})"});
                operators.Add(new Operator {type = AND, inputCount = 2, formatString = "({0} & {1})"});
                operators.Add(new Operator {type = SUBTRACT, inputCount = 2, formatString = "({0} - {1})"});

                inputValues.Add(new InputValue {type = INPUT_X, formatString = "x"});
                inputValues.Add(new InputValue {type = INPUT_Y, formatString = "y"});
                inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                inputValues.Add(new InputValue {type = INPUT_TIME, formatString = "t"});
                inputValues.Add(new InputValue {type = INPUT_ANGLE, formatString = "ang"});
                inputValues.Add(new InputValue {type = INPUT_LENGTH, formatString = "len"});
            }

            // More colorful "Cool circular mixed with xy, lots of variation and curves"
            if (false) {
                operators.Add(new Operator {type = XOR, inputCount = 2, formatString = "({0} ^ {1})"});
                operators.Add(new Operator {type = ADD, inputCount = 2, formatString = "({0} + {1})"});
                operators.Add(new Operator {type = MULTIPLY, inputCount = 2, formatString = "({0} * {1})"});
                operators.Add(new Operator {type = OR, inputCount = 2, formatString = "({0} | {1})"});
                operators.Add(new Operator {type = AND, inputCount = 2, formatString = "({0} & {1})"});
                operators.Add(new Operator {type = SUBTRACT, inputCount = 2, formatString = "({0} - {1})"});

                inputValues.Add(new InputValue {type = INPUT_X, formatString = "x"});
                inputValues.Add(new InputValue {type = INPUT_Y, formatString = "y"});
                inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                inputValues.Add(new InputValue {type = INPUT_TIME, formatString = "t"});
                inputValues.Add(new InputValue {type = INPUT_TIME, formatString = "t"});
                inputValues.Add(new InputValue {type = INPUT_ANGLE, formatString = "ang"});
                inputValues.Add(new InputValue {type = INPUT_LENGTH, formatString = "len"});
            }

            // With distorted polar coordinates only. Lots of organic stuff.
            if (true) { // 3: used this 4: used this with image
                operators.Add(new Operator {type = XOR, inputCount = 2, formatString = "({0} ^ {1})"});
                operators.Add(new Operator {type = ADD, inputCount = 2, formatString = "({0} + {1})"});
                operators.Add(new Operator {type = MULTIPLY, inputCount = 2, formatString = "({0} * {1})"});
                operators.Add(new Operator {type = OR, inputCount = 2, formatString = "({0} | {1})"});
                operators.Add(new Operator {type = AND, inputCount = 2, formatString = "({0} & {1})"});
                operators.Add(new Operator {type = SUBTRACT, inputCount = 2, formatString = "({0} - {1})"});
                operators.Add(new Operator {type = ANGLE_DISTORT, inputCount = 2, formatString = "angExt({0}, {1})"});
                operators.Add(new Operator {type = LENGTH_DISTORT, inputCount = 2, formatString = "lenExt({0}, {1})"});

                inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                inputValues.Add(new InputValue {type = INPUT_TIME, formatString = "t"});

                // operators.Add(new Operator {type = INPUT_TEXTURE_DISTORT, inputCount = 3, formatString = "sample({0}, {1})"});
                // operators.Add(new Operator {type = INPUT_TEXTURE_DISTORT, inputCount = 3, formatString = "sample({0}, {1})"});
                // inputValues.Add(new InputValue {type = INPUT_TEXTURE_RGB, formatString = "rgb"});
                // inputValues.Add(new InputValue {type = INPUT_TEXTURE_RGB, formatString = "rgb"});
            }

            // With distorted polar coordinates and other older input coordinates
            if (false) { // 1: used this, 5: used this with image
                operators.Add(new Operator {type = XOR, inputCount = 2, formatString = "({0} ^ {1})"});
                operators.Add(new Operator {type = ADD, inputCount = 2, formatString = "({0} + {1})"});
                operators.Add(new Operator {type = MULTIPLY, inputCount = 2, formatString = "({0} * {1})"});
                operators.Add(new Operator {type = OR, inputCount = 2, formatString = "({0} | {1})"});
                operators.Add(new Operator {type = AND, inputCount = 2, formatString = "({0} & {1})"});
                operators.Add(new Operator {type = SUBTRACT, inputCount = 2, formatString = "({0} - {1})"});
                operators.Add(new Operator {type = ANGLE_DISTORT, inputCount = 2, formatString = "angExt({0}, {1})"});
                operators.Add(new Operator {type = LENGTH_DISTORT, inputCount = 2, formatString = "lenExt({0}, {1})"});

                inputValues.Add(new InputValue {type = INPUT_X, formatString = "x"});
                inputValues.Add(new InputValue {type = INPUT_Y, formatString = "y"});
                inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                inputValues.Add(new InputValue {type = INPUT_TIME, formatString = "t"});
                inputValues.Add(new InputValue {type = INPUT_TIME, formatString = "t"});
                //inputValues.Add(new InputValue {type = INPUT_TIME, formatString = "t"});
                // inputValues.Add(new InputValue {type = INPUT_ANGLE, formatString = "ang"});
                // inputValues.Add(new InputValue {type = INPUT_LENGTH, formatString = "len"});

                // operators.Add(new Operator {type = INPUT_TEXTURE_DISTORT, inputCount = 3, formatString = "sample({0}, {1})"});
                // operators.Add(new Operator {type = INPUT_TEXTURE_DISTORT, inputCount = 3, formatString = "sample({0}, {1})"});
                // inputValues.Add(new InputValue {type = INPUT_TEXTURE_RGB, formatString = "rgb"});
                // inputValues.Add(new InputValue {type = INPUT_TEXTURE_RGB, formatString = "rgb"});
            }


            if (false) {
                operators.Add(new Operator {type = XOR, inputCount = 2, formatString = "({0} ^ {1})"});
                operators.Add(new Operator {type = ADD, inputCount = 2, formatString = "({0} + {1})"});
                operators.Add(new Operator {type = MULTIPLY, inputCount = 2, formatString = "({0} * {1})"});
                //operators.Add(new Operator {type = SQRT, inputCount = 1, formatString = "({0} ** 0.5)"});
                // operators.Add(new Operator {type = SQRT, inputCount = 1, formatString = "({0} ** 0.5)"});
                operators.Add(new Operator {type = OR, inputCount = 2, formatString = "({0} | {1})"});
                operators.Add(new Operator {type = AND, inputCount = 2, formatString = "({0} & {1})"});
                operators.Add(new Operator {type = SUBTRACT, inputCount = 2, formatString = "({0} - {1})"});
                // operators.Add(new Operator {type = INPUT_TEXTURE_DISTORT, inputCount = 3, formatString = "sample({0}, {1})"});
                // operators.Add(new Operator {type = INPUT_TEXTURE_DISTORT, inputCount = 3, formatString = "sample({0}, {1})"});
                // operators.Add(new Operator {type = INPUT_TEXTURE_DISTORT, inputCount = 3, formatString = "sample({0}, {1})"});
                // operators.Add(new Operator {type = SINE, inputCount = 1, formatString = "sin({0})"});
                // operators.Add(new Operator {type = TAN, inputCount = 1, formatString = "tan({0})"});
                // operators.Add(new Operator {type = SINE, inputCount = 1, formatString = "sin({0})"});
                // operators.Add(new Operator {type = TAN, inputCount = 1, formatString = "tan({0})"});

                inputValues.Add(new InputValue {type = INPUT_X, formatString = "x"});
                inputValues.Add(new InputValue {type = INPUT_Y, formatString = "y"});
                inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                // inputValues.Add(new InputValue {type = INPUT_CHANNEL, formatString = "c"});
                inputValues.Add(new InputValue {type = INPUT_TIME, formatString = "t"});
                inputValues.Add(new InputValue {type = INPUT_ANGLE, formatString = "ang"});
                inputValues.Add(new InputValue {type = INPUT_LENGTH, formatString = "len"});
                // inputValues.Add(new InputValue {type = INPUT_TEXTURE_RGB, formatString = "rgb"});
                // inputValues.Add(new InputValue {type = INPUT_TEXTURE_RGB, formatString = "rgb"});
                // inputValues.Add(new InputValue {type = INPUT_TEXTURE_RGB, formatString = "rgb"});
            }
        }

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

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1)) {
            updateTextures = true;
            time = 0;
            for (int i = 0; i < generations.Length; i++) {
                Generation generation = generations[i];

                Debug.Log("Generation " + i);

                RenderTexture texture = generation.texture;
                
                Node rootNode = CreateAst(15); // 2: changed 10 to 15
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
                    cs.SetTexture(kernelIndex, "InputTexture", inputTexture);
                    cs.SetBuffer(kernelIndex, "ProgramBuffer", generation.programBuffer);
                    cs.SetInt("ProgramBufferElementCount", generation.programBuffer.count);
                    cs.SetInt("Time", time);
                    cs.Dispatch(kernelIndex, threadGroups.x, threadGroups.y, threadGroups.z);
                }
            }
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
            programList.Add(node.inputValue.type); // "Set input" operator id
            uint memoryIndex = nextOpenMemoryIndex++;
            programList.Add(memoryIndex);
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
        string s = "";
        for (int y = 0; y < generationsY; y++) {
            for (int x = 0; x < generationsX; x++) {
                if (i < generations.Length) {
                    RenderTexture texture = generations[i].texture;
                    int x1 = gap + x * (texture.width + gap) + (Screen.width / 2 - totalWidth / 2);
                    int y1 = gap + y * (texture.height + gap) + (Screen.height / 2 - totalHeight / 2);
                    GUI.skin.box.normal.background = cellBackgroundTexture;
                    GUI.Box(new Rect(x1 - outline, y1 - outline, texture.width + outline * 2, texture.height + outline * 2), GUIContent.none);
                    GUI.DrawTexture(new Rect(x1, y1, texture.width, texture.height), texture);
                    s += i + ": " + generations[i].text + "\n";
                }
                i++;
            }
        }

        if (!videoMode) {
            float textAreaWidth = Screen.width / 2 - totalWidth / 2 - gap - gap;
            float textAreaHeight = Screen.height - gap * 2;

            GUI.skin.textArea.focused.background = null;
            GUI.skin.textArea.hover.background = null;

            GUI.skin.textArea.alignment = TextAnchor.UpperLeft;
            GUI.skin.textArea.normal.background = null;
            GUI.skin.textArea.normal.textColor = new Color(0f, 0f, 0f, 0.4f);
            GUI.TextArea(new Rect(gap, gap, textAreaWidth, textAreaHeight), s);
            
            s = "Operators:\n";
            foreach (Operator _operator in operators) {
                s += _operator.formatString + "\n";
            }
            s += "\nInputs:\n";
            foreach (InputValue inputValue in inputValues) {
                s += inputValue.formatString + "\n";
            }
            GUI.skin.textArea.normal.background = null;
            GUI.skin.textArea.normal.textColor = new Color(0f, 0f, 0f, 0.4f);
            GUI.skin.textArea.alignment = TextAnchor.UpperRight;
            GUI.TextArea(new Rect(Screen.width - 1 - gap - textAreaWidth, gap, textAreaWidth, textAreaHeight), s);

            if (showCpuVersion) {
                GUI.DrawTexture(new Rect(264f, 0f, 256f, 256f), cpuTexture);
            }
        }
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
            return node.inputValue.formatString;
        } else {
            return "N/A";
        }
    }
}
