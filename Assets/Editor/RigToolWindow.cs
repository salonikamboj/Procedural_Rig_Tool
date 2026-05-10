using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class RigToolWindow : EditorWindow
{
    private Transform rootJoint;
    private Transform endJoint;
    private List<Transform> detectedChain = new List<Transform>();
    private List<GameObject> generatedControls = new List<GameObject>();
    private GameObject controlsGroup;
    private GameObject ikTarget;
    private IKSolver ikSolver;
    private ProceduralBehavior proceduralBehavior;
    private Vector2 scrollPosition;



    [MenuItem("Tools/Rig Tool")]
    public static void OpenWindow()
    {
        RigToolWindow window = GetWindow<RigToolWindow>();
        window.titleContent = new GUIContent("Rig Tool");
        window.minSize = new Vector2(300, 400);
        window.Show();
    }

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("Rig Tool", EditorStyles.boldLabel);
        GUILayout.Space(10);
        GUILayout.Space(5);
        if (GUILayout.Button("Reset Tool"))
        {
            ResetTool();
        }
        GUILayout.Space(10);
        GUILayout.Label("1. Select Your Chain", EditorStyles.boldLabel);
        GUILayout.Space(5);

        rootJoint = (Transform)EditorGUILayout.ObjectField(
            "Root Joint",
            rootJoint,
            typeof(Transform),
            true
        );

        endJoint = (Transform)EditorGUILayout.ObjectField(
            "End Joint",
            endJoint,
            typeof(Transform),
            true
        );

        GUILayout.Space(5);

        if (GUILayout.Button("Use Selected as Root"))
        {
            if (Selection.activeTransform != null)
                rootJoint = Selection.activeTransform;
            else
                Debug.LogWarning("Nothing selected in the scene.");
        }

        if (GUILayout.Button("Use Selected as End"))
        {
            if (Selection.activeTransform != null)
                endJoint = Selection.activeTransform;
            else
                Debug.LogWarning("Nothing selected in the scene.");
        }

        GUILayout.Space(10);

        if (GUILayout.Button("Detect Chain"))
        {
            DetectChain();
        }

        GUILayout.Space(10);

        if (detectedChain.Count > 0)
        {
            GUILayout.Label("Detected Chain:", EditorStyles.boldLabel);

            for (int i = 0; i < detectedChain.Count; i++)
            {
                if (i < detectedChain.Count - 1)
                    GUILayout.Label("  " + detectedChain[i].name + "  →");
                else
                    GUILayout.Label("  " + detectedChain[i].name);
            }

            GUILayout.Space(5);
            GUILayout.Label("Total joints: " + detectedChain.Count, EditorStyles.miniLabel);
        }

        
        if (GUILayout.Button("Add Chain Visuals"))
        {
            AddJointVisuals();
            AddChainLine();
        }
        GUILayout.Space(15);
        GUILayout.Label("2. Control Generation", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("Generate Controls"))
        {
            GenerateControls();
        }

        GUILayout.Space(5);

        if (GUILayout.Button("Apply Follow Constraints"))
        {
            ApplyFollowConstraints();
        }

        // Show what was generated
        if (generatedControls.Count > 0)
        {
            GUILayout.Space(10);
            GUILayout.Label("Generated Controls:", EditorStyles.boldLabel);

            foreach (GameObject ctrl in generatedControls)
            {
                if (ctrl != null)
                    GUILayout.Label("  " + ctrl.name, EditorStyles.miniLabel);
            }
        }

        GUILayout.Space(15);
        GUILayout.Label("3. IK / FK", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("Setup IK / FK"))
        {
            SetupIKFK();
        }

        GUILayout.Space(5);

        // Only show these controls if a solver exists
        if (ikSolver != null)
        {
            // IK Weight slider — this IS the IK/FK switch
            EditorGUILayout.LabelField("IK Weight  (0 = FK    1 = IK)");
            float newWeight = EditorGUILayout.Slider(ikSolver.ikWeight, 0f, 1f);

            if (newWeight != ikSolver.ikWeight)
            {
                Undo.RecordObject(ikSolver, "Change IK Weight");
                ikSolver.ikWeight = newWeight;

                // When fully FK, re-enable follow constraints
                // When IK is involved, disable them
                foreach (Transform joint in detectedChain)
                {
                    FollowConstraint fc = joint.GetComponent<FollowConstraint>();
                    if (fc != null)
                        fc.enabled = (newWeight == 0f);
                }
            }

            GUILayout.Space(5);

            // Quick switch buttons
            EditorGUILayout.BeginHorizontal();

            if (GUILayout.Button("Full FK"))
            {
                Undo.RecordObject(ikSolver, "Switch to FK");
                ikSolver.ikWeight = 0f;
                foreach (Transform joint in detectedChain)
                {
                    FollowConstraint fc = joint.GetComponent<FollowConstraint>();
                    if (fc != null) fc.enabled = true;
                }
            }

            if (GUILayout.Button("Full IK"))
            {
                Undo.RecordObject(ikSolver, "Switch to IK");
                ikSolver.ikWeight = 1f;
                foreach (Transform joint in detectedChain)
                {
                    FollowConstraint fc = joint.GetComponent<FollowConstraint>();
                    if (fc != null) fc.enabled = false;
                }
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Show current mode clearly
            if (ikSolver.ikWeight == 0f)
                EditorGUILayout.HelpBox("FK Mode — rotate joints manually", MessageType.Info);
            else if (ikSolver.ikWeight == 1f)
                EditorGUILayout.HelpBox("IK Mode — move IK_Target to pose", MessageType.Info);
            else
                EditorGUILayout.HelpBox("Blending FK + IK", MessageType.Info);
        }

        GUILayout.Space(15);
        GUILayout.Label("4. Procedural Behavior", EditorStyles.boldLabel);
        GUILayout.Space(5);

        if (GUILayout.Button("Setup Procedural Behavior"))
        {
            SetupProceduralBehavior();
        }

        // Only show controls if component exists
        if (proceduralBehavior != null)
        {
            GUILayout.Space(8);

            // --- Wave ---
            EditorGUILayout.LabelField("Wave Motion", EditorStyles.boldLabel);

            bool newWave = EditorGUILayout.Toggle("Enable Wave", proceduralBehavior.enableWave);
            if (newWave != proceduralBehavior.enableWave)
            {
                Undo.RecordObject(proceduralBehavior, "Toggle Wave");
                proceduralBehavior.enableWave = newWave;
            }

            if (proceduralBehavior.enableWave)
            {
                float newAmp = EditorGUILayout.Slider(
                    "Amplitude", proceduralBehavior.waveAmplitude, 0f, 2f);
                if (newAmp != proceduralBehavior.waveAmplitude)
                {
                    Undo.RecordObject(proceduralBehavior, "Wave Amplitude");
                    proceduralBehavior.waveAmplitude = newAmp;
                }

                float newFreq = EditorGUILayout.Slider(
                    "Frequency", proceduralBehavior.waveFrequency, 0.1f, 5f);
                if (newFreq != proceduralBehavior.waveFrequency)
                {
                    Undo.RecordObject(proceduralBehavior, "Wave Frequency");
                    proceduralBehavior.waveFrequency = newFreq;
                }

                float newPhase = EditorGUILayout.Slider(
                    "Phase Offset", proceduralBehavior.wavePhaseOffset, 0f, 2f);
                if (newPhase != proceduralBehavior.wavePhaseOffset)
                {
                    Undo.RecordObject(proceduralBehavior, "Wave Phase");
                    proceduralBehavior.wavePhaseOffset = newPhase;
                }
            }

            GUILayout.Space(8);

            // --- Lag ---
            EditorGUILayout.LabelField("Smooth Follow / Lag", EditorStyles.boldLabel);

            bool newLag = EditorGUILayout.Toggle("Enable Lag", proceduralBehavior.enableLag);
            if (newLag != proceduralBehavior.enableLag)
            {
                Undo.RecordObject(proceduralBehavior, "Toggle Lag");
                proceduralBehavior.enableLag = newLag;
            }

            if (proceduralBehavior.enableLag)
            {
                float newLagAmt = EditorGUILayout.Slider(
                    "Lag Amount", proceduralBehavior.lagAmount, 0f, 0.99f);
                if (newLagAmt != proceduralBehavior.lagAmount)
                {
                    Undo.RecordObject(proceduralBehavior, "Lag Amount");
                    proceduralBehavior.lagAmount = newLagAmt;
                }
            }

            GUILayout.Space(8);

            // --- Spring ---
            EditorGUILayout.LabelField("Secondary Motion / Spring", EditorStyles.boldLabel);

            bool newSpring = EditorGUILayout.Toggle(
                "Enable Spring", proceduralBehavior.enableSpring);
            if (newSpring != proceduralBehavior.enableSpring)
            {
                Undo.RecordObject(proceduralBehavior, "Toggle Spring");
                proceduralBehavior.enableSpring = newSpring;
            }

            if (proceduralBehavior.enableSpring)
            {
                float newStiff = EditorGUILayout.Slider(
                    "Stiffness", proceduralBehavior.springStiffness, 1f, 30f);
                if (newStiff != proceduralBehavior.springStiffness)
                {
                    Undo.RecordObject(proceduralBehavior, "Spring Stiffness");
                    proceduralBehavior.springStiffness = newStiff;
                }

                float newDamp = EditorGUILayout.Slider(
                    "Damping", proceduralBehavior.springDamping, 0.5f, 0.99f);
                if (newDamp != proceduralBehavior.springDamping)
                {
                    Undo.RecordObject(proceduralBehavior, "Spring Damping");
                    proceduralBehavior.springDamping = newDamp;
                }
            }

            GUILayout.Space(8);

            // Show active behaviors clearly
            string activeMsg = "Active: ";
            if (!proceduralBehavior.enableWave &&
                !proceduralBehavior.enableLag &&
                !proceduralBehavior.enableSpring)
                activeMsg += "None";
            else
            {
                if (proceduralBehavior.enableWave) activeMsg += "[Wave] ";
                if (proceduralBehavior.enableLag) activeMsg += "[Lag] ";
                if (proceduralBehavior.enableSpring) activeMsg += "[Spring] ";
            }

            EditorGUILayout.HelpBox(activeMsg, MessageType.Info);
        }
        EditorGUILayout.EndScrollView();

    }

    private void DetectChain()
    {
        detectedChain.Clear();

        if (rootJoint == null || endJoint == null)
        {
            Debug.LogWarning("Assign both a Root and End joint first.");
            return;
        }

        Transform current = endJoint;

        while (current != null)
        {
            detectedChain.Insert(0, current);

            if (current == rootJoint)
                break;

            current = current.parent;
        }

        if (detectedChain[0] != rootJoint)
        {
            detectedChain.Clear();
            Debug.LogWarning("End joint is not a child of Root joint.");
        }
    }

    private void AddJointVisuals()
    {
        foreach (Transform joint in detectedChain)
        {
            // Don't add if already has one
            if (joint.Find("Visual") != null) continue;

            GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "Visual";
            visual.transform.SetParent(joint);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);

            // Remove collider — we don't need physics on this
            DestroyImmediate(visual.GetComponent<SphereCollider>());

            // Color it so it looks different from controls
            Renderer r = visual.GetComponent<Renderer>();
            r.material = new Material(Shader.Find("Sprites/Default"));
            r.material.color = new Color(1f, 0.5f, 0f); // orange

            Undo.RegisterCreatedObjectUndo(visual, "Add Joint Visual");
        }

        Debug.Log("Joint visuals added.");
    }

    private void AddChainLine()
    {
        // Remove old one if exists
        GameObject old = GameObject.Find("ChainLine");
        if (old != null) DestroyImmediate(old);

        GameObject lineObj = new GameObject("ChainLine");
        Undo.RegisterCreatedObjectUndo(lineObj, "Add Chain Line");

        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.positionCount = detectedChain.Count;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;
        lr.material = new Material(Shader.Find("Sprites/Default"));
        lr.material.color = Color.yellow;
        lr.useWorldSpace = true;

        // Attach an updater so the line follows joints at runtime
        ChainLineUpdater updater = lineObj.AddComponent<ChainLineUpdater>();
        updater.joints = new System.Collections.Generic.List<Transform>(detectedChain);
        updater.lr = lr;
    }

    private void GenerateControls()
    {
        if (detectedChain.Count == 0)
        {
            Debug.LogWarning("No chain detected. Run Detect Chain first.");
            return;
        }

        // Clear any previously generated controls
        if (controlsGroup != null)
        {
            DestroyImmediate(controlsGroup);
        }

        generatedControls.Clear();

        // Create a parent group to keep the scene organised
        controlsGroup = new GameObject("Controls");
        Undo.RegisterCreatedObjectUndo(controlsGroup, "Generate Controls");

        // Loop through every joint in the chain
        for (int i = 0; i < detectedChain.Count; i++)
        {
            Transform joint = detectedChain[i];

            // --- Create the Offset group ---
            GameObject offset = new GameObject("CTRL_" + joint.name + "_Offset");
            Undo.RegisterCreatedObjectUndo(offset, "Generate Controls");
            offset.transform.SetParent(controlsGroup.transform);

            // Place offset exactly where the joint is
            offset.transform.position = joint.position;
            offset.transform.rotation = joint.rotation;

            // --- Create the Control itself ---
            GameObject ctrl = new GameObject("CTRL_" + joint.name);
            Undo.RegisterCreatedObjectUndo(ctrl, "Generate Controls");
            ctrl.transform.SetParent(offset.transform);

            // Zero out local — sits exactly inside offset
            ctrl.transform.localPosition = Vector3.zero;
            ctrl.transform.localRotation = Quaternion.identity;

            // Add a visual so you can see it in the scene
            AddControlVisual(ctrl, i);

            generatedControls.Add(ctrl);
        }

        Debug.Log("Generated " + generatedControls.Count + " controls.");
    }

    private void AddControlVisual(GameObject ctrl, int index)
    {
        // We use a LineRenderer to draw a circle shape
        LineRenderer lr = ctrl.AddComponent<LineRenderer>();

        lr.useWorldSpace = false; // positions are local to the control
        lr.loop = true;
        lr.positionCount = 32;
        lr.startWidth = 0.05f;
        lr.endWidth = 0.05f;

        // Pick a colour based on position in chain
        // Root = green, End = red, Middle = cyan
        if (index == 0)
            lr.material = CreateLineMaterial(Color.green);
        else if (index == detectedChain.Count - 1)
            lr.material = CreateLineMaterial(Color.red);
        else
            lr.material = CreateLineMaterial(Color.cyan);

        // Draw the circle in local space
        float radius = 0.3f;
        for (int i = 0; i < 32; i++)
        {
            float angle = i * Mathf.PI * 2f / 32f;
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;
            lr.SetPosition(i, new Vector3(x, y, 0f));
        }
    }

    private Material CreateLineMaterial(Color color)
    {
        // Built-in Unity unlit shader — always visible, no lighting needed
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = color;
        return mat;
    }

    private void ApplyFollowConstraints()
    {
        if (detectedChain.Count == 0 || generatedControls.Count == 0)
        {
            Debug.LogWarning("Generate controls first.");
            return;
        }

        for (int i = 0; i < detectedChain.Count; i++)
        {
            Transform joint = detectedChain[i];
            GameObject ctrl = generatedControls[i];

            // Add our custom constraint component to the joint
            FollowConstraint fc = joint.gameObject.GetComponent<FollowConstraint>();
            if (fc == null)
                fc = joint.gameObject.AddComponent<FollowConstraint>();

            fc.target = ctrl.transform;
        }

        Debug.Log("Follow constraints applied.");
    }

    private void SetupIKFK()
    {
        if (detectedChain.Count == 0)
        {
            Debug.LogWarning("Detect a chain first.");
            return;
        }

        // --- Create IK Target ---
        // Place it where the end joint is
        Transform endJoint = detectedChain[detectedChain.Count - 1];

        ikTarget = new GameObject("IK_Target");
        Undo.RegisterCreatedObjectUndo(ikTarget, "Setup IK");
        ikTarget.transform.position = endJoint.position;

        // --- Create the solver on a new GameObject ---
        GameObject solverObj = new GameObject("IK_Solver");
        Undo.RegisterCreatedObjectUndo(solverObj, "Setup IK");

        ikSolver = solverObj.AddComponent<IKSolver>();

        // Give the solver the full joint chain
        ikSolver.joints = new System.Collections.Generic.List<Transform>(detectedChain);

        // Give it the target
        ikSolver.ikTarget = ikTarget.transform;

        // Start at full IK weight
        ikSolver.ikWeight = 1f;

        // Initialise bone lengths
        ikSolver.ForceInitialise();

        Debug.Log("IK/FK setup complete. Move IK_Target to pose the chain.");
        foreach (Transform joint in detectedChain)
        {
            FollowConstraint fc = joint.GetComponent<FollowConstraint>();
            if (fc != null)
                fc.enabled = false;
        }

        Debug.Log("Follow constraints disabled — IK solver is now in control.");
    }

    private void SetupProceduralBehavior()
    {
        if (detectedChain.Count == 0)
        {
            Debug.LogWarning("Detect a chain first.");
            return;
        }

        // Remove old one if it exists
        GameObject old = GameObject.Find("ProceduralBehavior");
        if (old != null)
        {
            DestroyImmediate(old);
        }

        GameObject procObj = new GameObject("ProceduralBehavior");
        Undo.RegisterCreatedObjectUndo(procObj, "Setup Procedural");

        proceduralBehavior = procObj.AddComponent<ProceduralBehavior>();
        proceduralBehavior.joints = new System.Collections.Generic.List<Transform>(detectedChain);

        Debug.Log("Procedural behavior ready. Hit Play and enable behaviors.");
    }

    private void ResetTool()
    {
        if (controlsGroup != null)
            DestroyImmediate(controlsGroup);

        GameObject ikSolverObj = GameObject.Find("IK_Solver");
        if (ikSolverObj != null)
            DestroyImmediate(ikSolverObj);

        GameObject ikTargetObj = GameObject.Find("IK_Target");
        if (ikTargetObj != null)
            DestroyImmediate(ikTargetObj);

        GameObject procObj = GameObject.Find("ProceduralBehavior");
        if (procObj != null)
            DestroyImmediate(procObj);

        GameObject chainLine = GameObject.Find("ChainLine");
        if (chainLine != null)
            DestroyImmediate(chainLine);

        // Remove follow constraints and visuals from joints
        foreach (Transform joint in detectedChain)
        {
            if (joint == null) continue;

            FollowConstraint fc = joint.GetComponent<FollowConstraint>();
            if (fc != null) DestroyImmediate(fc);

            Transform visual = joint.Find("Visual");
            if (visual != null) DestroyImmediate(visual.gameObject);
        }

        detectedChain.Clear();
        generatedControls.Clear();
        ikSolver = null;
        ikTarget = null;
        proceduralBehavior = null;
        rootJoint = null;
        endJoint = null;

        Debug.Log("Tool reset.");
    }
}