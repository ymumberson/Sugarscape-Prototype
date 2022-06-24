using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Agent : MonoBehaviour
{
    public enum Gender
    {
        Male, Female
    }

    public const float TURN_INTERVAL = 1f;
    public const float MAX_METABOLISM = 4;
    public const float MIN_METABOLISM = 1;
    public const int MAX_VISON = 10;
    public const int MIN_VISION = 1;
    public const float MAX_INITIAL_SUGAR = 100;
    public const float MIN_INITIAL_SUGAR = 50;
    public const int MAX_LIFESPAN = 100;
    public const int MIN_LIFESPAN = 60;
    public const float METABOLISM_POLLUTION_COEFFICIENT = 0.1f;
    public const int MALE_MIN_CHILDBEARING_AGE_LOWER = 12;
    public const int MALE_MAX_CHILDBEARING_AGE_LOWER = 50;
    public const int FEMALE_MIN_CHILDBEARING_AGE_LOWER = 12;
    public const int FEMALE_MAX_CHILDBEARING_AGE_LOWER = 40;
    public const int MALE_MIN_CHILDBEARING_AGE_UPPER = 15;
    public const int MALE_MAX_CHILDBEARING_AGE_UPPER = 60;
    public const int FEMALE_MIN_CHILDBEARING_AGE_UPPER = 15;
    public const int FEMALE_MAX_CHILDBEARING_AGE_UPPER = 50;

    [SerializeField] private float initial_sugar_endowment;
    [SerializeField] private float metabolism;
    [SerializeField] private int vision;
    [SerializeField] private Agent.Gender gender;
    [SerializeField] private float sugar; /* ie Wealth */
    [SerializeField] private Vector2 position;
    [SerializeField] private int LIFESPAN;
    [SerializeField] private int time_alive; /* ie Age */
    [SerializeField] private int min_childbearing_age;
    [SerializeField] private int max_childbearing_age;
    public bool isAlive = true;

    /*
     * Initialises metabolision and vision to random values between static bounds.
     */
    private void Awake()
    {
        metabolism = Random.Range(MIN_METABOLISM, MAX_METABOLISM);
        vision = Random.Range(MIN_VISION, MAX_VISON);
        position = Terrain.instance.setRandomPosition(this);
        transform.position = position;
        /* Initial endowment is made assuming everyone is gen 1, but assigned again later by parent if not gen 1 */
        initial_sugar_endowment = Random.Range(MIN_INITIAL_SUGAR, MAX_INITIAL_SUGAR);
        sugar = initial_sugar_endowment;
        LIFESPAN = Random.Range(MIN_LIFESPAN, MAX_LIFESPAN);
        time_alive = 0;
        gender = (Random.value > 0.5f) ? Agent.Gender.Male : Agent.Gender.Female;
        switch (gender)
        {
            case Agent.Gender.Male:
                min_childbearing_age = Random.Range(MALE_MIN_CHILDBEARING_AGE_LOWER, MALE_MIN_CHILDBEARING_AGE_UPPER);
                max_childbearing_age = Random.Range(MALE_MAX_CHILDBEARING_AGE_LOWER, MALE_MAX_CHILDBEARING_AGE_UPPER);
                break;
            case Agent.Gender.Female:
                min_childbearing_age = Random.Range(FEMALE_MIN_CHILDBEARING_AGE_LOWER, FEMALE_MIN_CHILDBEARING_AGE_UPPER);
                max_childbearing_age = Random.Range(FEMALE_MAX_CHILDBEARING_AGE_LOWER, FEMALE_MAX_CHILDBEARING_AGE_UPPER);
                break;
        }
    }

    public void setPosition(Vector2 pos)
    {
        Vector2 bounded_pos = Terrain.instance.getBoundedPosition(pos);
        Terrain.instance.removeAgent(this.position);
        this.position = bounded_pos;
        Terrain.instance.setAgent(this.position, this);
        transform.position = this.position;
    }

    /* To be called by the parent if agent is not gen 1 */
    public void setInitialSugarEndowment(float amount)
    {
        initial_sugar_endowment = amount;
        sugar = initial_sugar_endowment;
    }

    public void setMetabolism(float m)
    {
        metabolism = m;
    }

    public void setVision(int v)
    {
        vision = v;
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

    public void decrementWealth(float amount)
    {
        sugar -= amount;
    }

    public bool isFertile() /* Return (has at least initial sugar endowment) && (is within fertile age range) */
    {
        return sugar >= initial_sugar_endowment
            && time_alive >= min_childbearing_age && time_alive <= max_childbearing_age;
    }

    public void takeTurn()
    {
        move();
        collectSugar();
        eat();

        if (Terrain.instance.offspring_rules == Terrain.OffspringRules.Genetic_offspring
            && isFertile())
        {
            sexRuleS();
        } 

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
            //Terrain.instance.setAgent(position, null);
            //Terrain.instance.setAgent(pos, this);
            //this.position = Terrain.instance.getBoundedPosition(pos); //Stops assignment outside of the map
            setPosition(pos);
        }

        //transform.position = position;
    }

    public void collectSugar()
    {
        sugar += Terrain.instance.consumeSugar(position);
    }

    private void eat()
    {
        sugar -= metabolism;
        if (Terrain.instance.pollution_enabled) Terrain.instance.getTileInfo(position).incrementPollution(metabolism * METABOLISM_POLLUTION_COEFFICIENT);
        if (sugar <= 0) die();
    }

    private void sexRuleS()
    {
        List<Agent> neighbours = Terrain.instance.getNeighbouringAgents(position);
        if (neighbours.Count == 0) return; /* Fails if no neighbours */
        //Agent selected_neighbour = neighbours[Random.Range(0, neighbours.Count - 1)]; /* Select random neighbour */
        
        foreach (Agent neighbour in neighbours) //TODO Randomize the order of selection !!!!!!!
        {
            sexRuleS(neighbour);
        }
    }

    private void sexRuleS(Agent selected_neighbour)
    {
        if (!selected_neighbour.isFertile()) return; /* Fails if neighbour is not fertile */
        if (this.gender == selected_neighbour.gender) return; /* Fails if neighbour is same gender */
        Vector2 birth_location = Terrain.instance.getEmptyNeighbouringTile(position); /* See if this agent has an empty tile */
        Vector2 invalid_location = new Vector2(-1, -1);
        if (birth_location == invalid_location)
        { /* If this agent doesn't have an empty tile then try the neighbour */
            birth_location = Terrain.instance.getEmptyNeighbouringTile(selected_neighbour.position);
        }
        if (birth_location == invalid_location) return; /* No parents had an empty neighbouring tile so birth fails */
        birthChild(selected_neighbour, birth_location);
    }

    private void birthChild(Agent other_parent, Vector2 location)
    {
        /* Child gets half of both parents' initial endowments. Also decrement the parents wealth to give to child */
        float child_initial_sugar_endowment = this.initial_sugar_endowment / 2f + other_parent.initial_sugar_endowment / 2f;
        this.decrementWealth(this.initial_sugar_endowment / 2f);
        other_parent.decrementWealth(other_parent.initial_sugar_endowment / 2f);

        /* Create child */
        Agent child = Instantiate(Terrain.instance.AGENT_TEMPLATE).GetComponent<Agent>();
        child.setInitialSugarEndowment(child_initial_sugar_endowment); /* Give initial sugar */

        /* Set location */
        if (Terrain.instance.setAgent(location, child) == false) Destroy(child.gameObject); /* if tile was occupied then destroy child (ie something went wrong)*/
        //child.position = location;
        child.setPosition(location);

        /* Set genes to inherit from parents (50% chance per gene of inheriting from either parent)*/
        child.setMetabolism((Random.value > 0.5f) ? this.metabolism : other_parent.metabolism);
        child.setVision((Random.value > 0.5f) ? this.vision : other_parent.vision);

        int lsSize = Terrain.instance.getNumAgents();
        Terrain.instance.addAgent(child);
        int lsSize2 = Terrain.instance.getNumAgents();
        Debug.Log("Child created!!! " + lsSize + " -> " + lsSize2);
    }

    private void die()
    {
        Terrain.instance.removeAgent(position);
        //Destroy(this.gameObject);
        isAlive = false;
    }
}
