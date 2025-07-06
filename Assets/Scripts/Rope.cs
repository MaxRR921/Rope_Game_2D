using System.Collections.Generic;
using System.Linq;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;

public class Rope : MonoBehaviour
{
    [SerializeField]
    private List<Point> points = new List<Point>();
    [SerializeField]
    private List<Constraint> constraints = new List<Constraint>();

    [SerializeField]
    private float gravity = 10f;

    // Removed the "has_been_cut" field so that each frame we can cut again.
    // private bool has_been_cut = false;

    [SerializeField]
    private int numPoints = 10;

    [SerializeField]
    private float length;

    [SerializeField]
    private GameObject start_object;

    private Vector2 start_position;

    // <-- FIXED: store 3D positions for rendering
    private List<Vector3> pointPositions = new List<Vector3>();

    [SerializeField]
    private LineRenderer lineRenderer;

    [SerializeField]
    private float airFriction;

    [SerializeField]
    private List<Collider2D> collisionColliders = new List<Collider2D>();

    [SerializeField]
    private float ropeThickness = 0.1f;

    [SerializeField]
    private List<Rope> child_ropes = new List<Rope>();

    [SerializeField]
    private List<PinnableObject> pinnableObjects = new List<PinnableObject>();

    [SerializeField]
    bool loop = false;

    // When spawning a child, skip its InstantiateSections call
    [SerializeField]
    private bool skipInstantiate = false;

    [SerializeField]
    private GameObject entity;

    private PinnableObject lastPinnedClip;

    [SerializeField]
    int unpinCount = 0;





    void Start()
    {
        
        lastPinnedClip = null;

        // Initialize or grab existing LineRenderer
        lineRenderer = lineRenderer ?? gameObject.AddComponent<LineRenderer>();
        lineRenderer.generateLightingData = true;
        lineRenderer.shadowCastingMode = ShadowCastingMode.On;
        lineRenderer.receiveShadows = true;
        start_position = transform.position;

        // Create URP Lit material (Opaque)
        var mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        mat.SetFloat("_Surface", 0);  // 0 = Opaque
        mat.color = Color.blue;
        lineRenderer.material = mat;




        // Thickness
        lineRenderer.startWidth = lineRenderer.endWidth = 0.1f;

        // Only instantiate sections if this is not a spawned child
        if (!skipInstantiate)
        {
            InstantiateSections(numPoints);
        }

        UpdateLineRenderer();
        Debug.Log("HELLO!!");

        if (loop)
        {
            loopRope();
        }


    }


    void loopRope()
    {
        // 1) Find your two clips once
        PinnableObject hipClip1 = pinnableObjects
            .FirstOrDefault(o => o.getGameObject.name == "hipClip1");
        PinnableObject hipClip2 = pinnableObjects
            .FirstOrDefault(o => o.getGameObject.name == "hipClip2");

        if (hipClip1 == null || hipClip2 == null)
        {
            Debug.LogWarning("hipClip1 or hipClip2 not found!");
            return;
        }

        // 2) Pin points by i % 3
        int pinCount = 0;
        for (int i = 0; i < points.Count; i++)
        {
            if (i % 3 == 0)
            {
                points[i].Fix(true);
                var clip = (pinCount % 2 == 0) ? hipClip1 : hipClip2;
                points[i].setObjectPinnedTo(clip);
                pinCount++;
            }
        }
    }

    public void PinToClip(GameObject clipObj)
    {
        // Get the PinnableObject component on the clip
        PinnableObject newPin = new PinnableObject(0, clipObj);


        // Search backwards for the last point pinned to hipclip1/2
        for (int i = points.Count - 1; i >= 0; i--)
        {
            PinnableObject currentPin = points[i].getObjectPinnedTo;
            if (currentPin != null &&
                (currentPin.getGameObject.name == "hipClip1" ||
                 currentPin.getGameObject.name == "hipClip2"))
            {
                points[i].setObjectPinnedTo(newPin);
                points[i].Fix(true);
                unpinCount = 0;
                lastPinnedClip = newPin;
                Debug.Log("LAST PINNED CLIP IS: " + lastPinnedClip.getGameObject.name);
                Debug.Log($"Pinned point {points[i].getPid()} to new clip {clipObj.name}");
                return;
            }
        }

        Debug.LogWarning("No hip-clipped point found to re-pin.");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // When player (or rope) enters a clip trigger, re-pin the last hip-clipped point
        if (other.CompareTag("clip"))
        {
            PinToClip(other.gameObject);
        }
    }


    private void unravelRope()
    {
        if (lastPinnedClip == null) return;

        // find the last real hip‐clip index
        int idxToCountFrom = -1;
        for (int i = points.Count - 1; i >= 0; i--)
        {
            var pin = points[i].getObjectPinnedTo;
            if (pin != null)
            {
                string nm = pin.getGameObject.name;
                if (nm == "hipClip1" || nm == "hipClip2")
                {
                    idxToCountFrom = i;
                    Debug.Log("IDX to count from" + idxToCountFrom);
                    break;
                }
            }
        }

        if (idxToCountFrom < 0) return;

        // sum the actual rope‐path length from that pin to the player
        float ropeDistance = 0f;
        // stop at points.Count-1 so points[i+1] is always valid
        for (int j = idxToCountFrom; j < points.Count - 1; j++)
        {
            ropeDistance = Vector2.Distance(
                points[j].getPosition(),
                points[j + 1].getPosition()

            );
            Debug.Log("ROPE DISTANCE: " + ropeDistance +"points distance" +  Vector2.Distance(points[j].getPosition(), points[j + 1].getPosition()));

            if (ropeDistance > .7f)
            {
                // unpin the next hip‐clip
                for (int i = points.Count - 1; i >= 0; i--)
                {
                    var pin = points[i].getObjectPinnedTo;
                    if (pin != null)
                    {
                        string nm = pin.getGameObject.name;
                        if (nm == "hipClip1" || nm == "hipClip2")
                        {
                            points[i].setObjectPinnedTo(null);
                            points[i].Fix(false);
                            points[i].setPreviousPosition(points[i].getPosition());
                            unpinCount++;
                            return;
                        }
                    }
                }
            }
        }
        Debug.Log("FINAL  DISTANCE: " + ropeDistance);

        // only unpin once the *path* has stretched past threshold
        Debug.Log("2 * unpincount: " + 3f * unpinCount);
  
    }



    private void ravelRope()
    {
        if (lastPinnedClip != null)
        {
            Debug.Log("NOT NULL" + lastPinnedClip.getGameObject.name);
            if (Vector2.Distance(entity.transform.position, lastPinnedClip.getGameObject.transform.position) < (1.5 * unpinCount))
            {
                unpinCount++;
                for (int i = points.Count - 1; i >= 0; i--)
                {
                    PinnableObject currentPin = points[i].getObjectPinnedTo;
                    if (currentPin != null &&
                        (currentPin.getGameObject.name == "hipClip1" ||
                         currentPin.getGameObject.name == "hipClip2"))
                    {
                        points[i].setObjectPinnedTo(null);
                        points[i].Fix(false);
                        unpinCount++;
                        points[i].setPreviousPosition(points[i].getPosition());
                        return;
                    }
                }

            }
        }
    }

    void Update()
    {
        // Only update visuals here
        UpdateLineRenderer();
    }

    void FixedUpdate()
    {
        // Sync transforms so Collider2D matches moved Transforms
        Physics2D.SyncTransforms();
        unravelRope();
        Simulate();
    }

    [System.Serializable]
    class Point
    {
        private int m_pid;
        private Vector2 m_position;
        private Vector2 m_previous_position;
        private bool m_fix;
        private float m_friction;
        private bool m_is_colliding_AABB;
        private int m_collide_count_AABB;
        private bool m_is_colliding;
        private int m_collide_count;
        private PinnableObject m_object_pinned_to;

        public Point(int pid, Vector2 position, Vector2 previous_position, bool fix)
        {
            m_pid = pid;
            m_position = position;
            m_previous_position = previous_position;
            m_fix = fix;
            m_friction = 0.0f;
            m_is_colliding_AABB = false;
            m_collide_count_AABB = 0;
            m_is_colliding = false;
            m_collide_count = 0;
            m_object_pinned_to = null;
        }
        public Vector2 getPosition() => m_position;
        public PinnableObject getObjectPinnedTo => m_object_pinned_to;
        public void setObjectPinnedTo(PinnableObject object_pinned_to) => m_object_pinned_to = object_pinned_to;
        public int getPid() => m_pid;
        public void setPid(int pid) => m_pid = pid;
        public Vector2 getPreviousPosition() => m_previous_position;
        public bool isFix() => m_fix;
        public void Fix(bool fix) => m_fix = fix;
        public void setPosition(Vector2 position) => m_position = position;
        public void setPreviousPosition(Vector2 position) => m_previous_position = position;
        public void setFriction(float friction) => m_friction = friction;
        public float getFriction() => m_friction;
        public bool isColliding() => m_is_colliding;
        public void setIsColliding(bool colliding) => m_is_colliding = colliding;
        public int getCollideCount() => m_collide_count;
        public void setCollideCount(int collide_count) => m_collide_count = collide_count;
    }

    [System.Serializable]
    class Constraint
    {
        public Point m_pointA;
        public Point m_pointB;
        public float m_length;

        public Constraint(Point pointA, Point pointB)
        {
            m_pointA = pointA;
            m_pointB = pointB;
            m_length = Vector2.Distance(pointA.getPosition(), pointB.getPosition());
        }
    }

    [System.Serializable]
    class PinnableObject
    {
        [SerializeField]
        private GameObject m_gameObject;
        [SerializeField]
        private int id;

        public PinnableObject(int pid, GameObject obj)
        {
            m_gameObject = obj;
            id = pid;
        }
        public int getId() => id;
        public GameObject getGameObject => m_gameObject;
    }



    private void Simulate()
    {
        bool cutOccurred = false;

        foreach (Point p in points)
        {
            p.setFriction(1.0f);
            if (!p.isFix())
            {
                Vector2 prevPos = p.getPosition();
                Vector2 velocity = (prevPos - p.getPreviousPosition()) * airFriction * p.getFriction();
                Vector2 desiredPos = prevPos + velocity + Vector2.down * gravity * Time.fixedDeltaTime * Time.fixedDeltaTime;

                // --- circle‐cast collision check to prevent tunnelling ---
                Vector2 moveDelta = desiredPos - prevPos;
                if (moveDelta.sqrMagnitude > 0f)
                {
                    float moveDist = moveDelta.magnitude;

                    RaycastHit2D hit = Physics2D.CircleCast(prevPos, ropeThickness, moveDelta.normalized, moveDist);

                    if (hit.collider != null && collisionColliders.Contains(hit.collider) && hit.collider.enabled)
                    {
                        // mark & count collisions
                        if (!p.isColliding())
                        {
                            p.setIsColliding(true);
                            p.setCollideCount(1);
                        }
                        else
                        {
                            p.setCollideCount(p.getCollideCount() + 1);
                        }


                        // adjust friction
                        float newFric = p.getFriction() > 0.4f
                            ? 0.99f - (0.001f * p.getCollideCount())
                            : 0.99f;
                        p.setFriction(newFric);

                        // project out along normal
                        Vector2 normal = hit.normal;
                        desiredPos = hit.point + normal * ropeThickness;

                        // cutting logic
                        if (!cutOccurred && hit.collider.CompareTag("Cutter"))
                        {
                            hit.collider.enabled = false;
                            Debug.Log($"Cutting rope {this.gameObject.name}");
                            cutOccurred = true;
                            int cutIndex = points.IndexOf(p);
                            DecoupleAt(cutIndex);
                            return; // skip rest of this frame
                        }
                    }
                    else
                    {
                        p.setIsColliding(false);
                    }
                }

                // apply new position
                p.setPosition(desiredPos);
                p.setPreviousPosition(prevPos);
            }
            else if (p.getObjectPinnedTo != null)
            {
                p.setPosition(p.getObjectPinnedTo.getGameObject.transform.position);
            }
        }

        // constraint solver
        for (int i = 0; i < 5; i++)
        {
            foreach (var c in constraints)
            {
                Vector2 delta = c.m_pointB.getPosition() - c.m_pointA.getPosition();
                float dist = delta.magnitude;
                float diff = (dist - c.m_length) / dist;
                if (!c.m_pointA.isFix())
                    c.m_pointA.setPosition(c.m_pointA.getPosition() + delta * 0.5f * diff);
                if (!c.m_pointB.isFix())
                    c.m_pointB.setPosition(c.m_pointB.getPosition() - delta * 0.5f * diff);
            }
        }
    }


    private void DecoupleAt(int index)
    {
        if (index < 0 || index >= points.Count - 1) return;

        // Split points
        List<Point> lowerPoints = points.GetRange(index + 1, points.Count - (index + 1));
        points.RemoveRange(index + 1, points.Count - (index + 1));

        // Remove constraints that reference removed points
        constraints.RemoveAll(c => !points.Contains(c.m_pointA) || !points.Contains(c.m_pointB));

        // Update this rope's renderer so it ends at the cut
        UpdateLineRenderer();

        // Spawn a new child rope for the dangling segment
        SpawnChildSegment(lowerPoints);
    }

    private void SpawnChildSegment(List<Point> segmentPoints)
    {
        GameObject child = new GameObject("RopeSegment");
        child.transform.parent = transform.parent;
        child.transform.position = Vector2.zero;

        Rope r = child.AddComponent<Rope>();

        // Prevent the new Rope's Start() from re-instantiating points
        r.skipInstantiate = true;

        // Copy settings
        r.gravity = gravity;
        r.airFriction = airFriction;
        r.ropeThickness = ropeThickness;
        r.collisionColliders = new List<Collider2D>(collisionColliders);
        r.length = 0f;

        // Provide existing points and rebuild constraints
        r.points = segmentPoints;
        r.numPoints = segmentPoints.Count;
        segmentPoints[0].setPreviousPosition(segmentPoints[0].getPosition());

        r.constraints = new List<Constraint>();
        for (int i = 0; i < segmentPoints.Count - 1; i++)
        {
            r.constraints.Add(new Constraint(segmentPoints[i], segmentPoints[i + 1]));
        }

        // Hook up a new LineRenderer on the child
        LineRenderer lr = child.AddComponent<LineRenderer>();
        lr.material = lineRenderer.material;
        lr.startWidth = lr.endWidth = lineRenderer.startWidth;
        lr.shadowCastingMode = ShadowCastingMode.On;
        lr.receiveShadows = true;
        r.lineRenderer = lr;
    }

    //------------------------------------------------------------------

    private void InstantiateSections(int numPoints)
    {
        Vector2 distance_y = new Vector2(0, length / numPoints);
        Debug.Log("DISTANCE Y" + distance_y);
        Point last_point = null;

        for (int i = 0; i < numPoints; i++)
        {
            Vector2 currentPosition = start_position + (distance_y * i);
            Point newPoint = new Point(i, currentPosition, currentPosition, false);

            foreach (PinnableObject p in pinnableObjects)
            {
                if (p.getId() == newPoint.getPid())
                {
                    newPoint.setObjectPinnedTo(p);
                    newPoint.Fix(true);
                }
            }

            points.Add(newPoint);
            if (i > 0)
            {
                constraints.Add(new Constraint(last_point, newPoint));
            }

            last_point = newPoint;
            pointPositions.Add(new Vector3(newPoint.getPosition().x, newPoint.getPosition().y, 0f));
        }
    }

    private void UpdateLineRenderer()
    {
        pointPositions.Clear();

        foreach (var p in points)
        {
            Vector2 p2 = p.getPosition();
            // sanitize:
            if (float.IsNaN(p2.x) || float.IsNaN(p2.y) ||
                float.IsInfinity(p2.x) || float.IsInfinity(p2.y))
                p2 = Vector2.zero;

            // lift to Vector3 on your Z-plane (e.g. 0)
            pointPositions.Add(new Vector3(p2.x, p2.y, 0f));
        }

        lineRenderer.positionCount = pointPositions.Count;
        // now matches Vector3[] signature
        lineRenderer.SetPositions(pointPositions.ToArray());
    }


    private bool IsValidVector2(Vector2 v)
    {
        return !(float.IsNaN(v.x) || float.IsNaN(v.y) ||
                 float.IsInfinity(v.x) || float.IsInfinity(v.y));
    }
}

