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
    public const int NUM_TAGS = 11;

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
    [SerializeField] private int[] tags;
    private Agent[] parents;
    public bool isAlive = true;
    private SpriteRenderer sprite_renderer;

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
        parents = new Agent[2]; /* Mother and father */
        tags = new int[NUM_TAGS];
        initialiseTags();
        sprite_renderer = GetComponent<SpriteRenderer>();
    }

    private void initialiseTags()
    {
        for (int i=0; i<NUM_TAGS; ++i)
        {
            tags[i] = (Random.value > 0.5f) ? 1 : 0;
        }
    }

    public int getTag(int index)
    {
        //if (index >= NUM_TAGS || index < 0) return - 1; /* out of bounds */ (A crash might be easier for debugging)
        return tags[index];
    }

    public void toggleTag(int index)
    {
        if (index >= NUM_TAGS || index < 0) return; /* out of bounds */
        if (tags[index] == 0)
        {
            tags[index] = 1;
        }
        else
        {
            tags[index] = 0;
        }
    }

    public void setTags(int[] tags)
    {
        this.tags = tags;
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

    public void incrementWealth(float amount)
    {
        sugar += amount;
        //Debug.Log("Inherited $" + amount + "!");
    }

    public Agent[] getParents()
    {
        return parents;
    }

    public void setParents(Agent[] parents)
    {
        this.parents = parents;
    }

    public void addParent(Agent parent)
    {
        if (parents[0] == null)
        {
            parents[0] = parent;
        }
        else
        {
            parents[1] = parent;
        }
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

        if (Terrain.instance.culture_enabled)
        {
            spreadCultureToNeighbours();
            updateCultureColour();
        }

        ++time_alive;
        if (Terrain.instance.lifespan_enabled && time_alive >= LIFESPAN) /* Die of old age */
        {
            die();
        }
    }

    private void searchForBestPosition(ref Vector2 best_pos, ref float best_val, ref int dist_to_best, int x_offset, int y_offset)
    {
        Vector2 pos;
        float current_val;
        for (int i=1; i<= vision; ++i)
        {
            pos = new Vector2(position.x + x_offset * i, position.y + y_offset*i);
            if (Terrain.instance.pollution_enabled)
            {
                current_val = Terrain.instance.getSugarLevel(pos) / (1f + Terrain.instance.getPollution(pos));
            }
            else
            {
                current_val = Terrain.instance.getSugarLevel(pos);
            }
            if (current_val > best_val && !Terrain.instance.isOccupied(pos))
            {
                best_val = current_val;
                best_pos = pos;
                dist_to_best = Mathf.Abs(i);
            }
            else if (current_val == best_val
              && Mathf.Abs(i) < dist_to_best && !Terrain.instance.isOccupied(pos))
            {
                best_pos = pos;
                dist_to_best = Mathf.Abs(i);
            }
        }
    }

    private void searchNorthForBestPosition(ref Vector2 best_pos, ref float best_val, ref int dist_to_best)
    {
        searchForBestPosition(ref best_pos, ref best_val, ref dist_to_best, 0, 1);
    }

    private void searchEastForBestPosition(ref Vector2 best_pos, ref float best_val, ref int dist_to_best)
    {
        searchForBestPosition(ref best_pos, ref best_val, ref dist_to_best, 1, 0);
    }

    private void searchSouthForBestPosition(ref Vector2 best_pos, ref float best_val, ref int dist_to_best)
    {
        searchForBestPosition(ref best_pos, ref best_val, ref dist_to_best, 0, -1);
    }

    private void searchWestForBestPosition(ref Vector2 best_pos, ref float best_val, ref int dist_to_best)
    {
        searchForBestPosition(ref best_pos, ref best_val, ref dist_to_best, -1, 0);
    }

    private void move()
    {
        Vector2 pos = position;
        int distance_to_best = 0;

        /* We can either use a constant value of -1 or the actual value. If using the actual value then movement becomes very static
         * because agents just stay on the best tile they can see. However with a constant value of -1 agents are forced to move which
         * forces exploration at the cost of potentially losing the tile you're on */
        float best_val = -1f; /* const val */
        //if (Terrain.instance.pollution_enabled) /* Actual value of the tile the agent is on */
        //{
        //    best_val = Terrain.instance.getSugarLevel(pos) / (1f + Terrain.instance.getPollution(pos));
        //} 
        //else
        //{
        //    best_val = Terrain.instance.getSugarLevel(pos);
        //}

        List<System.Action> method_ls = new List<System.Action>();
        method_ls.Add(() => searchNorthForBestPosition(ref pos, ref best_val, ref distance_to_best));
        method_ls.Add(() => searchEastForBestPosition(ref pos, ref best_val, ref distance_to_best));
        method_ls.Add(() => searchSouthForBestPosition(ref pos, ref best_val, ref distance_to_best));
        method_ls.Add(() => searchWestForBestPosition(ref pos, ref best_val, ref distance_to_best));
        shuffleList<System.Action>(method_ls);
        foreach (System.Action a in method_ls)
        {
            a();
        }
        setPosition(pos);
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

    private void shuffleList<T>(List<T> ls)
    {
        T temp;
        int randomIndex;
        for (int i=0; i<ls.Count; ++i)
        {
            temp = ls[i];
            randomIndex = Random.Range(0, ls.Count);
            ls[i] = ls[randomIndex];
            ls[randomIndex] = temp;
        }
    }

    private void sexRuleS()
    {
        List<Agent> neighbours = Terrain.instance.getNeighbouringAgents(position);
        if (neighbours.Count == 0) return; /* Fails if no neighbours */

        shuffleList<Agent>(neighbours); /* Shuffles the order of the neighbours list */
        foreach (Agent neighbour in neighbours)
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
        child.setParents(new Agent[]{ this, other_parent});

        /* Set location */
        if (Terrain.instance.setAgent(location, child) == false) Destroy(child.gameObject); /* if tile was occupied then destroy child (ie something went wrong)*/
        child.setPosition(location);

        /* Set genes to inherit from parents (50% chance per gene of inheriting from either parent)*/
        child.setMetabolism((Random.value > 0.5f) ? this.metabolism : other_parent.metabolism);
        child.setVision((Random.value > 0.5f) ? this.vision : other_parent.vision);

        /* Sets tags to inherit from parents (Same if parents agree, 50% chance if disagree) */
        int[] child_tags = new int[NUM_TAGS];
        for (int i=0; i<NUM_TAGS; ++i)
        {
            if (tags[i] == other_parent.tags[i])
            {
                child_tags[i] = tags[i];
            }
            else
            {
                child_tags[i] = (Random.value > 0.5f) ? tags[i] : other_parent.tags[i];
            }
        }
        child.setTags(child_tags);

        Terrain.instance.addAgent(child);
    }

    private void spreadCultureToNeighbours()
    {
        List<Agent> neighbours = Terrain.instance.getNeighbouringAgents(position);
        if (neighbours.Count == 0) return; /* No neighbours so fails */
        int random_index;
        foreach (Agent a in neighbours)
        {
            random_index = Random.Range(0, NUM_TAGS - 1);
            if (a.getTag(random_index) != this.tags[random_index]) a.toggleTag(random_index);
        }
    }

    private void die()
    {
        if (Terrain.instance.inheritance_enabled)
        {
            divideInheritanceEquallyBetweenChildren();
        }
        Terrain.instance.removeAgent(position);
        isAlive = false;
    }

    private void divideInheritanceEquallyBetweenChildren()
    {
        List<Agent> children = Terrain.instance.getChildren(this);
        if (children.Count == 0) return; /* ie has no children so return */
        float wealth_per_child = sugar / children.Count;
        foreach (Agent child in children)
        {
            child.incrementWealth(wealth_per_child);
        }
    }

    public void updateCultureColour()
    {
        int sum = 0;
        for (int i=0; i<NUM_TAGS; ++i)
        {
            sum += tags[i];
        }

        if (sum > NUM_TAGS / 2)
        {
            sprite_renderer.color = Color.red;
        }
        else
        {
            sprite_renderer.color = Color.blue;
        }
    }
}
