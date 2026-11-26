using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DragonManager : MonoBehaviour
{
    //This class allows us to make a list to track position and rotation simaltaneously
    public class Marker
    {
        public Vector3 position;
        public Quaternion rotation;
        public Marker(Vector3 pos, Quaternion rot)
        {
            position = pos;
            rotation = rot;
        }
    }
    List<Marker> markerList = new List<Marker>();

    //distanceBetweenBodyParts is a little misleading, it is the number of frames between the different bodyparts.
    //speed is the speed of the snake
    //turnspeed is the rate at which the snake can turn
    [SerializeField] int distanceBetweenBodyParts = 20;
    [SerializeField] float speed = 280;
    [SerializeField] float turnSpeed = 180;
    [SerializeField] List<GameObject> partsToSpawn = new List<GameObject>();
    public List<GameObject> dragonBody = new List<GameObject>();
    Rigidbody2D dragonHead;
    // MarkerManager markerM = new MarkerManager();

    // Start is called before the first frame update
    void Start()
    {
        GameObject temp = Instantiate(partsToSpawn[0], transform.position, transform.rotation, transform);
        if (!temp.GetComponent<Rigidbody2D>())
        {
            temp.AddComponent<Rigidbody2D>();
            temp.GetComponent<Rigidbody2D>().gravityScale = 0;
        }
        dragonBody.Add(temp);
        partsToSpawn.RemoveAt(0);
        // dragonHead = dragonBody[0];
        dragonHead = dragonBody[0].GetComponent<Rigidbody2D>();


    }

    // Update is called once per frame
    void FixedUpdate()
    {

        ManageBody();
        SnakeMovement();

    }
    // This function updates the snake's body as pieces are created or destroyed. 
    void ManageBody()
    {
        if (distanceBetweenBodyParts < 1)
            distanceBetweenBodyParts = 1;
        if (partsToSpawn.Count > 0)
            CreateBodyParts();
        for (int i = 0; i < dragonBody.Count; i++)
        {
            if (dragonBody[i] == null)
            {
                dragonBody.RemoveAt(i);
                i = i - 1;
                if (dragonBody.Count > 0 && dragonHead == null)
                {
                    dragonHead = dragonBody[0].GetComponent<Rigidbody2D>();
                    markerList.RemoveRange(0, distanceBetweenBodyParts);
                }
            }
        }
        if (dragonBody.Count == 0)
            Destroy(this);
        markerList.Insert(0, new Marker(dragonBody[0].transform.position, dragonBody[0].transform.rotation));
    }
    void SnakeMovement()
    {
        // Moves the snake's head with inputs.
        dragonHead.velocity = dragonBody[0].transform.right * speed * Time.deltaTime;
        if (Input.GetAxis("Horizontal") != 0)
            dragonBody[0].transform.Rotate(new Vector3(0, 0, -turnSpeed * Time.deltaTime * Input.GetAxis("Horizontal")));
        // transforms the remaining bodyparts of the snake to follow after the head. 
        for (int i = 1; i < dragonBody.Count; i++)
        {
            dragonBody[i].transform.position = markerList[i * distanceBetweenBodyParts - 1].position;
            dragonBody[i].transform.rotation = markerList[i * distanceBetweenBodyParts - 1].rotation;
        }
    }
    //Creates the different bodyparts given enough frames have elapsed.
    void CreateBodyParts()
    {
        if (partsToSpawn.Count > 0)
        {
            if (markerList.Count > dragonBody.Count * distanceBetweenBodyParts - 1)
            {
                int listNumber = dragonBody.Count * distanceBetweenBodyParts - 1;


                GameObject temp = Instantiate(partsToSpawn[0], markerList[listNumber].position, markerList[listNumber].rotation, transform);
                if (!temp.GetComponent<Rigidbody2D>())
                {
                    temp.AddComponent<Rigidbody2D>();
                    temp.GetComponent<Rigidbody2D>().gravityScale = 0;
                }
                dragonBody.Add(temp);
                partsToSpawn.RemoveAt(0);
            }
        }
    }
    //Referenced in different scripts if you want to spawn body parts dynamically in game. 
    public void AddBodyParts(GameObject obj)
    {
        partsToSpawn.Add(obj);
    }
    //This function is used to remove body parts from the snake.
    public void RemoveBodyParts(int index)
    {
        if (index < dragonBody.Count)
        {
            GameObject tempBody = dragonBody[index];
            dragonBody.RemoveAt(index);
            if (index == 0)
                dragonHead = dragonBody[0].GetComponent<Rigidbody2D>();
            Destroy(tempBody);
        }

    }
}
