using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Terrain : MonoBehaviour
{
    public enum GrowbackRules
    {
        G1,G2,G3
    }

    /* Settings */
    public GrowbackRules growback_rules = GrowbackRules.G1;
    public bool lifespan_enabled = true;
    public bool offspring_enabled = true;
    public bool pollution_enabled = true;
    public bool pollution_dispersion_enabled = true;
    public bool display_pollution = false;
    public bool display_sugar_pollution_ratio = false;

    public static Terrain instance;
    public GameObject AGENT_TEMPLATE;
    public GameObject TILE_TEMPLATE;
    public const int NUM_STARTING_AGENTS = 250;
    public const int HEIGHT = 50;
    public const int WIDTH = 50;
    public const float MAX_SUGAR = 4;
    public float TIME_INTERVAL = 0.1f;
    public const float HARVEST_POLLUTION_COEFFICIENT = 0.1f;
    public float MAX_POLLUTION_DISPLAYED = 50f;

    private TileInfo[,] map;
    public int agentsMoved;
    private List<Agent> agents;
    private float timer;

    /* Statistics */
    public int capacity;
    public float avg_vision;
    public float avg_metabolism;
    public float total_wealth;
    public int time_count;

    private void Awake()
    {
        if (instance)
        {
            Destroy(this);
        } else
        {
            instance = this;
        }

        /* Initialise map */
        map = new TileInfo[WIDTH,HEIGHT];
        for (int j=0; j<HEIGHT; ++j)
        {
            for (int i=0; i<WIDTH; ++i)
            {
                GameObject new_tile = Instantiate(TILE_TEMPLATE);
                new_tile.transform.position = new Vector2(i,j);
                map[i, j] = new_tile.GetComponent<TileInfo>();
                //map[i,j] = new TileInfo(MAX_SUGAR,new_tile.GetComponent<SpriteRenderer>());
            }
        }
        initialiseMap();

        /* Create agents */
        agents = new List<Agent>();
        for (int i=0; i<NUM_STARTING_AGENTS; ++i)
        {
            //Instantiate(AGENT_TEMPLATE);
            //agents[i] = Instantiate(AGENT_TEMPLATE).GetComponent<Agent>();
            agents.Add(Instantiate(AGENT_TEMPLATE).GetComponent<Agent>());
        }
        capacity = NUM_STARTING_AGENTS;
        agentsMoved = 0;
        time_count = 0;
    }

    public void resetSimulation()
    {
        for (int j=0; j<HEIGHT; ++j)
        {
            for (int i=0; i<WIDTH; ++i)
            {
                Destroy(getTileInfo(i, j).gameObject);
                GameObject new_tile = Instantiate(TILE_TEMPLATE);
                new_tile.transform.position = new Vector2(i, j);
                map[i, j] = new_tile.GetComponent<TileInfo>();
            }
        }
        initialiseMap();

        //Agent temp;
        for (int i=0; i<agents.Count; ++i) /* This function might crash */ // ====================================
        {
            //temp = agents[0];
            //agents.RemoveAt(0);
            //Destroy(temp.gameObject);

            Destroy(agents[i].gameObject);
        }
        agents.Clear();


        for (int i=0; i<NUM_STARTING_AGENTS; ++i)
        {
            agents.Add(Instantiate(AGENT_TEMPLATE).GetComponent<Agent>());
        }
        capacity = NUM_STARTING_AGENTS;
        agentsMoved = 0;
        time_count = 0;
    }

    private void initialiseMap()
    {
        /* Map values from: https://github.com/NetLogo/models/blob/master/Curricular%20Models/Mind%20the%20Gap/sugar-map.txt */
        int[] values = {0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,2,2,2,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,3,3,4,4,4,4,3,3,3,3,3,3,3,2,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,2,2,2,2,2,3,3,3,3,3,3,4,4,4,4,4,4,4,4,3,3,3,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,2,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,2,2,2,2,2,3,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,3,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,3,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,3,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,2,0,0,0,0,0,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,4,4,4,4,4,4,4,4,3,3,3,3,3,3,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,4,4,4,4,3,3,3,3,3,3,3,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,2,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,2,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,2,3,3,3,3,3,3,3,4,4,4,4,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,2,2,2,2,2,3,3,3,3,3,3,4,4,4,4,4,4,4,4,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,0,0,0,2,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,3,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,2,2,2,2,3,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,3,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,2,2,2,2,2,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,2,2,2,2,3,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,3,2,2,2,2,2,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,3,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,3,3,3,3,3,3,4,4,4,4,4,4,4,4,3,3,3,3,3,3,2,2,2,2,2,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,2,3,3,3,3,3,3,3,4,4,4,4,3,3,3,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,3,3,3,3,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,2,2,2,2,2,2,2,2,2,2,3,3,3,3,3,3,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,1,1,1,1,1,1,1,1,2,2,2,2,2,2,2,2,2,2,2,2,2,1,1,1,1,1,1,1,1,1,1,1,1,1,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};
        //Debug.Log("Values length: " + values.Length);
        for (int i=0; i<values.Length; ++i)
        {
            int y = i / WIDTH;
            int x = WIDTH-1-(i - y * WIDTH);
            map[x, y].setMaxSugar(values[i]);
            //Debug.Log(i + " -> " + x + "," + y);
        }
    }

    private void FixedUpdate()
    {
        /* Only regenerate map after all agents have taken their turn *//*
        if (agentsMoved >= NUM_AGENTS)
        {
            regenerateMap();
            agentsMoved = 0;
        }*/

        timer += Time.fixedDeltaTime;
        if (timer >= TIME_INTERVAL)
        {
            /* Reset stats before recounting */
            avg_metabolism = 0;
            avg_vision = 0;
            total_wealth = 0;

            //TODO randomise order of agents list!!
            randomizeAgentOrder();
            for (int i = 0; i < getNumAgents(); ++i)
            {
                if (agents[i] != null)
                {
                    if (agents[i].isAlive)
                    {
                        agents[i].takeTurn();
                        avg_vision += agents[i].getVision();
                        avg_metabolism += agents[i].getMetabolism();
                        total_wealth += agents[i].getWealth();
                    }
                    else
                    {
                        if (offspring_enabled)
                        {
                            /* Replace agents if dead */
                            replaceAgentWithNewRandomAgent(i);
                        }
                        else
                        {
                            /* Just removing agents if dead */
                            Destroy(agents[i].gameObject);
                            agents.RemoveAt(i);
                            --capacity;
                        }
                    }
                }
            }
            avg_metabolism /= capacity;
            avg_vision /= capacity;
            switch (growback_rules)
            {
                case GrowbackRules.G1:
                    regenerateMapToMax();
                    break;
                case GrowbackRules.G2:
                    regenerateMapByRandomAmount();
                    break;
                case GrowbackRules.G3:
                    regenerateMapSeasonal();
                    break;
            }

            if (pollution_dispersion_enabled) pollutionDispersionRule();

            timer = 0;
            ++time_count;
        }
    }

    public void randomizeAgentOrder()
    {
        Agent temp;
        int randomIndex;
        int num_agents = getNumAgents();
        for (int i=0; i< num_agents; ++i)
        {
            randomIndex = Random.Range(0, num_agents - 1);
            if (randomIndex != i)
            {
                temp = agents[i];
                agents[i] = agents[randomIndex];
                agents[randomIndex] = temp;
            }
        }
    }

    private void replaceAgentWithNewRandomAgent(int agentIndex)
    {
        Destroy(agents[agentIndex].gameObject);
        agents[agentIndex] = Instantiate(AGENT_TEMPLATE).GetComponent<Agent>();
    }

    public int getNumAgents()
    {
        if (agents == null) return 0;
        return agents.Count;
    }

    /**
     * G1
     */
    private void regenerateMapToMax()
    {
        for (int j = 0; j < HEIGHT; ++j)
        {
            for (int i = 0; i < WIDTH; ++i)
            {
                map[i, j].setSugarLevelToMax();
                //map[i, j].increaseSugarByRandomAmount();
            }
        }
        //Debug.Log("Regenerated Map.");
    }

    private void regenerateMapByRandomAmount()
    {
        for (int j = 0; j < HEIGHT; ++j)
        {
            for (int i = 0; i < WIDTH; ++i)
            {
                map[i, j].increaseSugarByRandomAmount();
            }
        }
        //Debug.Log("Regenerated Map.");
    }

    private void regenerateMapSeasonal()
    {
        bool summer_in_top_half = Mathf.Floor(time_count / 50f) % 2 == 0;
        float summer_increase = 1f;
        float winter_increase = summer_increase/8f;
        float top_half_increase, bottom_half_increase;
        if (summer_in_top_half)
        {
            top_half_increase = summer_increase;
            bottom_half_increase = winter_increase;
        } else
        {
            top_half_increase = winter_increase;
            bottom_half_increase = summer_increase;
        }
        for (int j = 0; j < HEIGHT/2; ++j)
        {
            for (int i = 0; i < WIDTH; ++i)
            {
                map[i, j].incrementSugarBy(bottom_half_increase);
            }
        }
        for (int j = HEIGHT/2; j < HEIGHT; ++j)
        {
            for (int i = 0; i < WIDTH; ++i)
            {
                map[i, j].incrementSugarBy(top_half_increase);
            }
        }
        //Debug.Log("Regenerated Map.");
    }

    public float getMaxSugar()
    {
        return MAX_SUGAR;
    }

    public Vector2 getRandomMapPosition()
    {
        return new Vector2(Random.Range(0, WIDTH - 1), Random.Range(0, HEIGHT - 1));
    }

    public Vector2 getRandomEmptyMapPosition()
    {
        /* Checks if map is full to avoid infinite loop of trying to find empty space on full map */
        if (getNumAgents() >= HEIGHT * WIDTH) return new Vector2(-1, -1); // (Should probs throw error), also currently unreachable but let's play it safe
        Vector2 pos = getRandomMapPosition();
        while (map[(int)(pos.x),(int)(pos.y)].isOccupied())
        {
            pos = getRandomMapPosition();
        }
        return pos;
    }

    /**
     * Accounts for wraparound so always puts x and y into bounds
     **/
    public TileInfo getTileInfo(int x, int y)
    {
        while (x < 0) x += WIDTH;
        while (y < 0) y += HEIGHT;
        while (y >= HEIGHT) y -= HEIGHT;
        while (x >= WIDTH) x -= WIDTH;
        return map[x, y];
    }

    public Vector2 getBoundedPosition(Vector2 pos)
    {
        float x = pos.x;
        float y = pos.y;
        while (x < 0) x += WIDTH;
        while (y < 0) y += HEIGHT;
        while (y >= HEIGHT) y -= HEIGHT;
        while (x >= WIDTH) x -= WIDTH;
        return new Vector2(x, y);
    }

    public TileInfo getTileInfo(float x, float y)
    {
        return getTileInfo((int)x, (int)y);
    }
    public TileInfo getTileInfo(Vector2 pos)
    {
        return getTileInfo((int)pos.x,(int)pos.y);
    }

    /**
     * Position can only be an empty tile
     **/
    public Vector2 setRandomPosition(Agent a)
    {
        Vector2 pos = getRandomEmptyMapPosition();
        getTileInfo(pos).agent = a;
        return pos;
    }

    public float getSugarLevel(Vector2 pos)
    {
        return getTileInfo(pos).getSugarLevel();
    }

    public void setSugarLevel(Vector2 pos, float new_sugar_level)
    {
        getTileInfo(pos).setSugarLevel(new_sugar_level);
    }

    public float consumeSugar(Vector2 pos)
    {
        float amount = getSugarLevel(pos);
        setSugarLevel(pos, 0);
        if (pollution_enabled)
        {
            getTileInfo(pos).incrementPollution(amount * HARVEST_POLLUTION_COEFFICIENT);
        }
        return amount;
    }

    public bool isOccupied(Vector2 pos)
    {
        return getTileInfo(pos).isOccupied();
    }

    public Agent getAgent(Vector2 pos)
    {
        return getTileInfo(pos).agent;
    }

    public void setAgent(Vector2 pos, Agent a)
    {
        getTileInfo(pos).agent = a;
    }

    public Color getColor(float sugar_level)
    {
        if (sugar_level <= 0) return Color.black;
        float perc = sugar_level / MAX_SUGAR;
        return new Color(1.0f*perc, 0.92f*perc, 0f);
    }

    public Color getPollutionColor(float pollution_amount)
    {
        if (pollution_amount <= 0) return Color.black;
        if (pollution_amount >= MAX_POLLUTION_DISPLAYED) return new Color(1f,0,0);
        float perc = pollution_amount / MAX_POLLUTION_DISPLAYED;
        return new Color(1f*perc,0,0);
    }

    public Color getPollutionToSugarColor(float pollution_amount, float sugar_amount)
    {
        float ratio = sugar_amount / (1f + pollution_amount);
        return new Color(0,ratio,0);
    }

    public float getPollution(Vector2 pos)
    {
        return getTileInfo(pos).getPollution();
    }

    public void pollutionDispersionRule()
    {
        for (int j=0; j<HEIGHT; ++j)
        {
            for (int i=0; i<WIDTH; ++i)
            {
                /* Flux is average of all von neuman neighbours */ //(Plus itself?)
                float flux = 0;
                flux += getTileInfo(i+1, j).getPollution();
                flux += getTileInfo(i-1, j).getPollution();
                flux += getTileInfo(i, j+1).getPollution();
                flux += getTileInfo(i, j-1).getPollution();
                flux += getTileInfo(i, j).getPollution();
                flux /= 5f;
                //flux /= 4;
                getTileInfo(i, j).setPollution(flux);
            }
        }
    }
}
