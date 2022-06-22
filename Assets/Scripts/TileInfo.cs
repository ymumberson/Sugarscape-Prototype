using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TileInfo : MonoBehaviour
{
    public Agent agent;
    private SpriteRenderer sprite_renderer;
    [SerializeField] public float MAX_SUGAR;
    [SerializeField] private float sugar_level;
    [SerializeField] private float pollution;

    /*public TileInfo(float MAX_SUGAR, SpriteRenderer tile_spriteRenderer)
    {
        this.MAX_SUGAR = Random.Range(0f, MAX_SUGAR);
        sugar_level = this.MAX_SUGAR;
        tile_spriteRenderer.color = Terrain.instance.getColor(sugar_level);
        this.tile_spriteRenderer = tile_spriteRenderer;
    }*/

    private void Awake()
    {
        this.MAX_SUGAR = Random.Range(0f, Terrain.instance.getMaxSugar());
        sugar_level = this.MAX_SUGAR;
        this.sprite_renderer = GetComponent<SpriteRenderer>();
        sprite_renderer.color = Terrain.instance.getColor(sugar_level);
        pollution = 0;
    }

    public bool isOccupied()
    {
        return this.agent != null;
    }

    public void setMaxSugar(float MAX_SUGAR)
    {
        this.MAX_SUGAR = MAX_SUGAR;
        setSugarLevel(MAX_SUGAR);
    }

    public void setSugarLevel(float new_sugar_level)
    {
        //Debug.Log("Sugar level: " + sugar_level + " -> " + new_sugar_level);
        sugar_level = new_sugar_level;
        Color oldCol = sprite_renderer.color;
        updateColor();
        //Debug.Log("Old colour: " + oldCol + " -> " + sprite_renderer.color);
    }

    public float getSugarLevel()
    {
        return sugar_level;
    }

    public void setSugarLevelToMax()
    {
        sugar_level = MAX_SUGAR;
        updateColor();
    }

    public void increaseSugarByRandomAmount()
    {
        sugar_level += Random.Range(0, MAX_SUGAR);
        if (sugar_level > MAX_SUGAR) sugar_level = MAX_SUGAR;
        updateColor();
    }

    public void incrementSugarBy(float amount)
    {
        sugar_level += amount;
        if (sugar_level > MAX_SUGAR) sugar_level = MAX_SUGAR;
        updateColor();
    }

    public void updateColor()
    {
        if (Terrain.instance.display_pollution)
        {
            sprite_renderer.color = Terrain.instance.getPollutionColor(pollution);
        }
        else if (Terrain.instance.display_sugar_pollution_ratio)
        {
            sprite_renderer.color = Terrain.instance.getPollutionToSugarColor(pollution, sugar_level);
        }
        else
        {
            sprite_renderer.color = Terrain.instance.getColor(sugar_level);
        }
    }

    public float getPollution()
    {
        return pollution;
    }

    public void setPollution(float pol)
    {
        pollution = pol;
    }

    public void incrementPollution(float amount)
    {
        pollution += amount;
    }
}
