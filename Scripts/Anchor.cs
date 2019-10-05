using Godot;
using System;
using System.Collections.Generic;

public class Anchor : RigidBody2D
{
    public Constructor constructor;
    private static int MAXBUILDLENGTH = 30;
    public bool canConnect() => true;
    private bool active = false;

    public List<Brace> braces = new List<Brace>();

    public override void _Ready()
    {
        Brace brace = (Brace)GetNode("Brace");
        brace.anchor = this;
        braces.Add(brace);
        setActive(false);
    }

    public void connect(Brace brace)
    {
        if (canConnect())
        {
            brace.anchor = this;
            AddChild(brace);
            brace.GlobalPosition = GetGlobalMousePosition();
            List<Brace> range = getBracesInRange(brace.GlobalPosition);
            if (range.Count > 0)
            {
                foreach(Brace b in range)
                {
                    if (b.canSee(brace))
                        b.connect(brace);
                }
                braces.Add(brace);
                CenterGravity();
            }
            else
            {
                brace.remove();
            }
        }
    }

    public void addBraceToList(Brace brace)
    {
        braces.Add(brace);
    }

    public void removeBraceFromList(Brace brace)
    {
        braces.Remove(brace);
        if (braces.Count == 0)
            destory();
        else
        {
            List<Brace> connected = new List<Brace>();
            braces[0].ConnectedBraces(connected);
            if (connected.Count != braces.Count)
            {
                Brace notConnectedBrace = null;
                List<Brace> notConnected = new List<Brace>();
                for(int i = 0; i < braces.Count; i++)  // TODO: lots of repeated checks
                {
                    if (!connected.Contains(braces[i]))
                    {
                        notConnectedBrace = braces[i];
                        notConnectedBrace.ConnectedBraces(notConnected);
                    }
                }
                foreach(Brace nbrace in notConnected)
                    braces.Remove(nbrace);
                
                GD.Print(braces.Count);
                constructor.newSumAnchor(notConnected, this);
                CenterGravity();
            }
        }
    }

    public int braceCount() => braces.Count;

    public void CenterGravity()
    {
        Vector2 center = CenterOfBraces();
        Vector2 currPos = GlobalPosition;
        Vector2 diff = center - currPos;
        this.GlobalPosition += diff;
        moveAllChildren(-diff);
        Mass = 1 * braces.Count;
    }

    public void setActive(bool value)
    {
        if (value) {
            active = true;
            this.Mode = ModeEnum.Rigid;
        } else {
            active = false;
            this.Mode = ModeEnum.Static;
        }
        foreach(Brace brace in braces)
                brace.setActive(value);
    }

    public void removeBrace(Vector2 pos)
    {
        Brace brace = getClosestBrace(pos);
        if (brace != null && brace.GlobalPosition.DistanceTo(pos) < 3)
        {
            brace.remove();
        }
    }

    public void destory()
    {
        constructor.removeAnchorFromList(this);
        this.QueueFree();
    }

    private Brace getClosestBrace(Vector2 pos)
    {
        if (braces == null || braces.Count == 0)
            return null;

        float minLength = 1000000;
        Brace closest = braces[0];
        foreach(Brace brace in braces)
        {
            float length = brace.GlobalPosition.DistanceTo(pos);
            if (length < minLength)
            {
                minLength = length;
                closest = brace;
            }
        }
        return closest;
    }

    private List<Brace> getBracesInRange(Vector2 pos)
    {
        List<Brace> inRange = new List<Brace>();
        foreach(Brace brace in braces)
        {
            if (brace.GlobalPosition.DistanceTo(pos) < MAXBUILDLENGTH)
                inRange.Add(brace);
        }
        return inRange;
    }

    private Vector2 CenterOfBraces()
    {
        Vector2 sum = Vector2.Zero;
        if (braces.Count <= 0)
            return sum;
        foreach(Brace brace in braces)
        {
            sum += brace.GlobalPosition;
        }
        return sum/braces.Count;
    }

    private void moveAllChildren(Vector2 diff)
    {
        foreach(Node2D node in GetChildren())
        {
            node.GlobalPosition += diff;
        }
    }
}
