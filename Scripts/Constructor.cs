using Godot;
using System;
using SC = System.Collections.Generic;
using GC = Godot.Collections;

public class Constructor : Node2D
{
    [Signal]
    public delegate void GooAmount();
    [Signal]
    public delegate void GooPlace();
    [Signal]
    public delegate void GooRemove();
    public int gooAmount = 0;
    public int maxGoo = 10;
    public Anchor activeAnchor;
    public Anchor startAnchor;
    private SC.List<Anchor> anchors = new SC.List<Anchor>();
    private bool hasActiveAnchor() => activeAnchor != null;

    public override void _Ready()
    {
        
    }

    public override void _Process(float delta)
    {
        if (hasActiveAnchor())
        {
            activeAnchor.Position = GetGlobalMousePosition();

            if (Input.IsActionJustReleased("spawn_anchor"))
            {
                if (rayCastinTerrain() || rayCastinPlayer() || rayCastinAnchor() || (startAnchor != null && startAnchor.getBracesInRange(activeAnchor.GlobalPosition).Count == 0))
                {
                    activeAnchor.destory();
                    activeAnchor = null;
                    startAnchor = null;
                    addGoo();
                }
                else // Placing anchor into world
                {
                    if (startAnchor != null && activeAnchor != null)
                    {
                        Brace brace = newBrace(GetGlobalMousePosition());
                        startAnchor.connect(brace);
                        activeAnchor.destory();
                        activeAnchor = null;
                        startAnchor = null;
                    }
                    else
                    {
                        activeAnchor.setActive(true);
                        activeAnchor.AddCentralForce(new Vector2(0, 0.1f));
                        anchors.Add(activeAnchor);
                        activeAnchor = null;
                    }
                }
            }
        }
        else // does not have active anchor
        {
            if (Input.IsActionJustPressed("spawn_anchor") && !rayCastinTerrain() && removeGoo())
            {
                activeAnchor = newAnchor(GetGlobalMousePosition());
                startAnchor = getRayCastAnchor();
            }
            else if (Input.IsActionJustPressed("remove_anchor"))
            {
                Anchor anchor = getRayCastAnchor();
                if (anchor != null && hasGooSpace())
                {
                     if (anchor.removeBrace(GetGlobalMousePosition()))
                        addGoo();
                }
            }
        }
    }

    public void removeAnchorFromList(Anchor anchor)
    {
        anchors.Remove(anchor);
    }

    public void newSumAnchor(SC.List<Brace> braces, Anchor preAnchor)
    {
        Anchor anchor = newAnchor(ToLocal(braces[0].GlobalPosition));
        anchor.GlobalPosition = preAnchor.GlobalPosition;
        anchor.Rotation = preAnchor.Rotation;
        anchor.SetAngularVelocity(preAnchor.GetAngularVelocity());
        anchor.SetLinearVelocity(preAnchor.GetLinearVelocity());
        anchors.Add(anchor);
        foreach(Brace brace in braces)
        {
            anchor.addBraceToList(brace);
            brace.anchor = anchor;
            brace.GetParent().RemoveChild(brace);
            anchor.AddChild(brace);
            brace.updateConnection();
        }
        anchor.braces[0].remove();
        anchor.setActive(true);
        anchor.CenterGravity();
    }

    private Anchor newAnchor(Vector2 position)
    {
        var anchorScene = GD.Load<PackedScene>("res://Scenes/Assets/Anchor.tscn");
        Anchor anchor = (Anchor) anchorScene.Instance();
        anchor.Position = position;  
        anchor.constructor = this;
        AddChild(anchor);
        return anchor;
    }

    private Brace newBrace(Vector2 position)
    {
        return (Brace) GD.Load<PackedScene>("res://Scenes/Assets/Brace.tscn").Instance();
    }

    private Anchor getRayCastAnchor() {
        var spaceState = GetWorld2d().GetDirectSpaceState();
        GC.Array result = spaceState.IntersectPoint(GetGlobalMousePosition(), 32, null, 4); // add collision layer

        Anchor anchor = null;
        foreach(GC.Dictionary dic in result)
        {
            anchor = (Anchor) dic["collider"];
        }        
        return anchor;
    }
    private bool rayCastinTerrain() {
        var spaceState = GetWorld2d().GetDirectSpaceState();
        var result = spaceState.IntersectPoint(GetGlobalMousePosition(), 32, null, 1); // add collision layer
        return result.Count != 0;
    }
    private bool rayCastinPlayer() {
        var spaceState = GetWorld2d().GetDirectSpaceState();
        var result = spaceState.IntersectPoint(GetGlobalMousePosition(), 32, null, 2); // add collision layer
        return result.Count != 0;
    }
    private bool rayCastinAnchor() {
        var spaceState = GetWorld2d().GetDirectSpaceState();
        var result = spaceState.IntersectPoint(GetGlobalMousePosition(), 32, null, 4); // add collision layer
        return result.Count != 0;
    }

    private Anchor getClosestAnchor() {
        float minDistance = 10000;
        Anchor closest = null;
        foreach(Anchor anchor in anchors)
        {
            float distance = anchor.getClosestDistance(GetGlobalMousePosition());
            if (distance < minDistance)
            {
                minDistance = distance;
                closest = anchor;
            }
        }
        return closest;
    }

    public void SpawnGoo(Vector2 position)
    {
        Random ran = new Random();
        Vector2 offset = new Vector2(ran.Next(-5, 5), ran.Next(-5, 5));
        Anchor anchor = newAnchor(position + offset);
        anchor.setActive(true);
        anchor.AddCentralForce(new Vector2(0, ran.Next(-3, -1)));
        anchors.Add(anchor);
    }

    public bool addGoo()
    {
        if (hasGooSpace())
        {
            gooAmount++;
            EmitSignal("GooRemove");
            EmitSignal("GooAmount", gooAmount);
            return true;
        }
        return false;
    }

    public bool removeGoo()
    {
        if (gooAmount - 1 >= 0)
        {
            gooAmount--;
            EmitSignal("GooPlace");
            EmitSignal("GooAmount", gooAmount);
            return true;
        }
        return false;
    }

    public bool hasGooSpace() => gooAmount + 1 <= maxGoo;
}
