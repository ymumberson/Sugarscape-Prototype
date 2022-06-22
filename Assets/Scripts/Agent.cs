using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    public const float TURN_INTERVAL = 1f;
    public const float MAX_METABOLISM = 4;
    public const float MIN_METABOLISM = 1;
    public const int MAX_VISON = 10;
    public const int MIN_VISION = 1;
    public const float MAX_INITIAL_SUGAR = 8;
    public const float MIN_INITIAL_SUGAR = 4;
    public const int MAX_LIFESPAN = 100;
    public const int MIN_LIFESPAN = 60;
    public const float METABOLISM_POLLUTION_COEFFICIENT = 0.1f;

    [SerializeField] private float metabolism;
    [SerializeField] private int vision;
    [SerializeField] private float sugar; /* ie Wealth */
    [SerializeField] private Vector2 position;
    [SerializeField] private int LIFESPAN;
    [SerializeField] private int time_alive;
    public bool isAlive = true;
    private float timer;

    /*
     * Initialises metabolision and vision to random values between static bounds.
     */
    private void Awake()
    {
        metabolism = Random.Range(MIN_METABOLISM, MAX_METABOLISM);
        vision = Random.Range(MIN_VISION, MAX_VISON);
        position = Terrain.instance.setRandomPosition(this);
        transform.position = position;
        sugar = Random.Range(MIN_INITIAL_SUGAR, MAX_INITIAL_SUGAR);
        timer = 0;
        LIFESPAN = Random.Range(MIN_LIFESPAN, MAX_LIFESPAN);
        time_alive = 0;
    }

    public float getVision()
    {
        return vision;
    }

    public float getMetabolism()
    {
        return metabolism;
    }

    public float getWealth()
    {
        return sugar;
    }

/*    private void FixedUpdate()
    {
        timer += Time.fixedDeltaTime;
        if (timer >= TURN_INTERVAL)
        {
            move();
            collectSugar();
            eat();
            ++Terrain.instance.agentsMoved;
            timer = 0;
        }
    }*/

    public void takeTurn()
    {
        move();
        collectSugar();
        eat();

        ++time_alive;
        if (Terrain.instance.lifespan_enabled && time_alive >= LIFESPAN) /* Die of old age */
        {
            die();
        }
    }

    private void move()
    {
        float current_tile_sugar_level = Terrain.instance.getSugarLevel(position);
        float highest_sugar_level = -1;
        Vector2 pos = position;
        Vector2 temp_pos;
        float temp_sugar_level;
        int distance_to_tile = 0;

        //TODO Randomize the order directions are checked in!!!!
        /* Check for movement in x direction */
        for (int i=-vision; i<=vision; ++i)
        {
            temp_pos = new Vector2(position.x + i, position.y);
            //temp_sugar_level = Terrain.instance.getSugarLevel(temp_pos);
            if (Terrain.instance.pollution_enabled)
            {
                temp_sugar_level = Terrain.instance.getSugarLevel(temp_pos) / (1f + Terrain.instance.getPollution(temp_pos));
            }
            else
            {
                temp_sugar_level = Terrain.instance.getSugarLevel(temp_pos);
            }
            if (temp_sugar_level > highest_sugar_level && !Terrain.instance.isOccupied(temp_pos))
            {
                highest_sugar_level = temp_sugar_level;
                pos = temp_pos;
                distance_to_tile = Mathf.Abs(i);
            } else if (temp_sugar_level == highest_sugar_level 
                && Mathf.Abs(i) < distance_to_tile && !Terrain.instance.isOccupied(temp_pos))
            {
                pos = temp_pos;
                distance_to_tile = Mathf.Abs(i);
            }
        }
        /* Check for movement in y direction */
        for (int i = -vision; i <= vision; ++i)
        {
            temp_pos = new Vector2(position.x, position.y + i);
            //temp_sugar_level = Terrain.instance.getSugarLevel(temp_pos);
            if (Terrain.instance.pollution_enabled)
            {
                temp_sugar_level = Terrain.instance.getSugarLevel(temp_pos) / (1f + Terrain.instance.getPollution(temp_pos));
            }
            else
            {
                temp_sugar_level = Terrain.instance.getSugarLevel(temp_pos);
            }
            if (temp_sugar_level > highest_sugar_level && !Terrain.instance.isOccupied(temp_pos))
            {
                highest_sugar_level = temp_sugar_level;
                pos = temp_pos;
                distance_to_tile = Mathf.Abs(i);
            }
            else if (temp_sugar_level == highest_sugar_level
              && Mathf.Abs(i) < distance_to_tile && !Terrain.instance.isOccupied(temp_pos))
            {
                pos = temp_pos;
                distance_to_tile = Mathf.Abs(i);
            }
        }

        /* Check a better tile was found than the current tile */
        if (highest_sugar_level > -1)
        {
            Terrain.instance.setAgent(position, null);
            Terrain.instance.setAgent(pos, this);
            this.position = Terrain.instance.getBoundedPosition(pos); //Stops assignment outside of the map
        }

        transform.position = position;
    }

    public void collectSugar()
    {
        sugar += Terrain.instance.consumeSugar(position);
    }

    private void eat()
    {
        sugar -= metabolism;
        Terrain.instance.getTileInfo(position).incrementPollution(metabolism * METABOLISM_POLLUTION_COEFFICIENT);
        if (sugar <= 0) die();
    }

    private void die()
    {
        Terrain.instance.setAgent(position, null);
        //Destroy(this.gameObject);
        isAlive = false;
    }
}
