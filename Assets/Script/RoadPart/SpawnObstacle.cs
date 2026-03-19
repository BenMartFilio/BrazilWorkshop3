using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnObstacle : MonoBehaviour
{
    public GameObject[] obstacles;
    public float minDelay = 1f;
    public float maxDelay = 3f;
    public bool isSpawning = false;

    private Coroutine spawning;

    private List<GameObject> poolObject = new List<GameObject>();
    private List<int> shuffleBag = new List<int>();

    private float generalSpeed = 0;



    [SerializeField] private TimeManager _timeManager;
    [SerializeField] private GameObject[] _fallingLines;

    [SerializeField] private int _spawnDelayDuration = 3;
    private int randomNumber;


    private void OnEnable()
    {
        _timeManager.OnTimePassed += SpawnRate;
    }

    private void OnDisable()
    {
        _timeManager.OnTimePassed -= SpawnRate;
    }

    private void SpawnRate()
    {
        generalSpeed = Mathf.Clamp(generalSpeed + 1, 0, 30);
        for (int i = 0; i < obstacles.Length; i++)
        {
            obstacles[i].GetComponent<ScrollingElement>().UpdateSpeed(generalSpeed);
        }
    }

    IEnumerator SpawnRoutine()
    {
        while (isSpawning)
        {
            float delay = Random.Range(minDelay, maxDelay);
            yield return new WaitForSeconds(delay);

            Spawn();
        }
    }

    void Spawn()
    {
        int index = GetNextObstacleIndex();

        GameObject obj = GetFromPool(obstacles[index]);

        randomNumber = Random.Range(0, _fallingLines.Length);

        obj.transform.SetPositionAndRotation(new Vector3(_fallingLines[randomNumber].transform.position.x, transform.position.y, transform.position.z), obstacles[index].transform.rotation);
        obj.SetActive(true);
     //   obj.GetComponent<ScrollingElement>().UpdateSpeed(generalSpeed);
    }

    int GetNextObstacleIndex()
    {
        if (shuffleBag.Count == 0)
        {
            for (int i = 0; i < obstacles.Length; i++)
            {
                shuffleBag.Add(i);
            }

            for (int i = 0; i < shuffleBag.Count; i++)
            {
                int aleatoire = Random.Range(i, shuffleBag.Count);
                (shuffleBag[i], shuffleBag[aleatoire]) = (shuffleBag[aleatoire], shuffleBag[i]);
            }
        }

        int index = shuffleBag[0];
        shuffleBag.RemoveAt(0);

        return index;
    }

    GameObject GetFromPool(GameObject prefab)
    {
        foreach (GameObject obj in poolObject)
        {
            if (!obj.activeInHierarchy && obj.name.Contains(prefab.name))
            {
                return obj;
            }
        }

        GameObject newObj = Instantiate(prefab);
        poolObject.Add(newObj);

        return newObj;
    }

    public void StartSpawning()
    {
        isSpawning = true;

        spawning ??= StartCoroutine(SpawnRoutine());
    }

    public void StopSpawning()
    {
        isSpawning = false;

        if (spawning != null)
        {
            StopCoroutine(spawning);
            spawning = null;
        }
        foreach (GameObject b in poolObject)
        {
            b.GetComponent<ScrollingElement>().StopMoving();
        }
    }


    private void Start()
    {
            StartSpawning();
    }
}