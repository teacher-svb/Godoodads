namespace Godoodads.Tests.Curves;

using System.Linq;
using Godot;
using GdUnit4;
using Godoodads.Curves.Adapters;
using Godoodads.Curves.Core;
using static GdUnit4.Assertions;

// Brief godoocurves §4.3, §4.4: writing shapes into Curve3D, tilt from Godot's own up vectors, Append.
[TestSuite]
public class CurveWriterTests
{
    private static ShapeCurve Local(ShapeKind kind, ShapeParameters p)
    {
        var result = Shapes.Build(kind, p);
        AssertThat(result.Error).IsNull();
        return result.Curve;
    }

    private static Curve3D Write(ShapeKind kind, ShapeParameters p) => CurveWriter.Create(Local(kind, p).ToPath(ShapeFrame.Default));

    private static float Degrees(Vector3 a, Vector3 b) => Mathf.RadToDeg(a.AngleTo(b));

    private static float Offset(Curve3D curve, int point) => curve.GetClosestOffset(curve.GetPointPosition(point));

    [TestCase]
    [RequireGodotRuntime]
    public void Points_handles_and_the_closed_flag_are_written_exactly()
    {
        var shape = Local(ShapeKind.Ellipse, new ShapeParameters()).ToPath(ShapeFrame.Default);
        var curve = CurveWriter.Create(shape);
        AssertThat(curve.PointCount).IsEqual(shape.Points.Count);
        AssertThat(curve.Closed).IsTrue();
        AssertThat(curve.UpVectorEnabled).IsTrue();
        for (int i = 0; i < shape.Points.Count; i++)
        {
            AssertThat(curve.GetPointPosition(i)).IsEqual(shape.Points[i].Position);
            AssertThat(curve.GetPointIn(i)).IsEqual(shape.Points[i].In);
            AssertThat(curve.GetPointOut(i)).IsEqual(shape.Points[i].Out);
        }
        AssertThat(Write(ShapeKind.Straight, new ShapeParameters()).Closed).IsFalse();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_loop_with_side_shift_leaves_upright()
    {
        // Brief §10.2: without tilt, Godot's up is 19° off after a 1 m Side Shift.
        var curve = Write(ShapeKind.Loop, new ShapeParameters { Length = 16, Diameter = 6, SideShift = 1 });
        float length = curve.GetBakedLength();
        AssertThat(Degrees(curve.SampleBakedUpVector(length, true), Vector3.Up) < 0.5f).IsTrue();
        AssertThat(Degrees(curve.SampleBakedUpVector(length, false), Vector3.Up) > 5f).IsTrue();
        // At the top of the loop the up faces down, to the centre.
        var top = curve.GetClosestOffset(new Vector3(8, 6, 0.5f));
        AssertThat(Degrees(curve.SampleBakedUpVector(top, true), Vector3.Down) < 1f).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_corkscrew_faces_its_axis()
    {
        var curve = Write(ShapeKind.Corkscrew, new ShapeParameters { Length = 12, Radius = 2, Turns = 1 });
        int checkedPoints = 0;
        float worstAtPoints = 0, worstBetween = 0;
        for (int i = 0; i < curve.PointCount; i++)
        {
            var p = curve.GetPointPosition(i);
            var tangent = (curve.GetPointOut(i).LengthSquared() > 0 ? curve.GetPointOut(i) : -curve.GetPointIn(i)).Normalized();
            var toAxis = new Vector3(p.X, 0, 0) - p;
            var across = toAxis - tangent * toAxis.Dot(tangent);
            if (across.Length() < 0.4f)
                continue;
            worstAtPoints = Mathf.Max(worstAtPoints, Degrees(curve.SampleBakedUpVector(Offset(curve, i), true), across));
            checkedPoints++;
        }
        float length = curve.GetBakedLength();
        for (int s = 0; s <= 200; s++)
        {
            float offset = length * s / 200;
            var p = curve.SampleBaked(offset);
            var toAxis = new Vector3(p.X, 0, 0) - p;
            var tangent = (curve.SampleBaked(Mathf.Min(offset + 0.01f, length)) - curve.SampleBaked(Mathf.Max(offset - 0.01f, 0))).Normalized();
            var across = toAxis - tangent * toAxis.Dot(tangent);
            if (across.Length() > 0.4f)
                worstBetween = Mathf.Max(worstBetween, Degrees(curve.SampleBakedUpVector(offset, true), across));
        }
        GD.Print($"Corkscrew: {curve.PointCount} points, worst {worstAtPoints:F3}° at points, {worstBetween:F3}° between them.");
        AssertThat(checkedPoints >= 4).IsTrue();
        AssertThat(worstAtPoints < 0.5f).IsTrue();
        AssertThat(worstBetween < 3f).IsTrue();
        AssertThat(Degrees(curve.SampleBakedUpVector(length, true), Vector3.Up) < 0.5f).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void A_banked_turn_reaches_its_bank_into_the_turn()
    {
        var curve = Write(ShapeKind.BankedTurn, new ShapeParameters { Radius = 10, AngleDegrees = 90, BankDegrees = 30 });
        int middle = curve.PointCount / 2;
        var up = curve.SampleBakedUpVector(Offset(curve, middle), true);
        AssertThat(Mathf.Abs(Degrees(up, Vector3.Up) - 30) < 0.5f).IsTrue();
        var toCentre = new Vector3(0, 0, 10) - curve.GetPointPosition(middle);
        AssertThat(up.Dot(toCentre) > 0).IsTrue();
        AssertThat(Degrees(curve.SampleBakedUpVector(0, true), Vector3.Up) < 0.5f).IsTrue();
        AssertThat(Degrees(curve.SampleBakedUpVector(curve.GetBakedLength(), true), Vector3.Up) < 0.5f).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Append_joins_smoothly_and_leaves_the_original_alone()
    {
        ShapeFrame.TryCreate(new Vector3(1, 2, 3), new Vector3(0, 0, -1), Vector3.Up, out var frame, out _);
        var original = CurveWriter.Create(Local(ShapeKind.Arc, new ShapeParameters { Radius = 6, AngleDegrees = 70 }).ToPath(frame));
        var before = Enumerable.Range(0, original.PointCount).Select(i => (original.GetPointPosition(i), original.GetPointIn(i), original.GetPointOut(i))).ToArray();
        CurveWriter.TryEndFrame(original, out var end);

        var result = CurveWriter.Append(original, Local(ShapeKind.Loop, new ShapeParameters { SideShift = 1 }));
        AssertThat(result.Error).IsNull();
        var curve = result.Curve;
        AssertThat(curve == original).IsFalse();
        AssertThat(original.PointCount).IsEqual(before.Length);
        for (int i = 0; i < before.Length; i++)
            AssertThat((original.GetPointPosition(i), original.GetPointIn(i), original.GetPointOut(i))).IsEqual(before[i]);
        AssertThat(original.GetPointOut(before.Length - 1)).IsEqual(Vector3.Zero);

        int join = before.Length - 1;
        AssertThat(curve.GetPointPosition(join)).IsEqual(original.GetPointPosition(join));
        AssertThat(curve.GetPointIn(join)).IsEqual(original.GetPointIn(join));
        AssertThat(Degrees(-curve.GetPointIn(join), curve.GetPointOut(join)) < 0.01f).IsTrue();
        AssertThat(Degrees(curve.GetPointOut(join), end.Forward) < 0.01f).IsTrue();
        AssertThat(curve.PointCount).IsEqual(before.Length + 7 - 1);
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Append_carries_the_end_up_over()
    {
        var curve = Write(ShapeKind.Straight, new ShapeParameters { Length = 10 });
        curve.SetPointTilt(1, 0.5f);
        var endUp = curve.SampleBakedUpVector(curve.GetBakedLength(), true);
        var appended = CurveWriter.Append(curve, Local(ShapeKind.Straight, new ShapeParameters { Length = 6 })).Curve;
        AssertThat(appended.PointCount).IsEqual(3);
        AssertThat(appended.GetPointPosition(2).IsEqualApprox(new Vector3(16, 0, 0))).IsTrue();
        AssertThat(Degrees(appended.SampleBakedUpVector(appended.GetBakedLength(), true), endUp) < 0.5f).IsTrue();
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Closed_shapes_and_closed_curves_cannot_be_appended()
    {
        var open = Write(ShapeKind.Straight, new ShapeParameters());
        AssertThat(CurveWriter.Append(open, Local(ShapeKind.Circle, new ShapeParameters())).Error).StartsWith("Closed shapes cannot be appended");
        var closed = Write(ShapeKind.Circle, new ShapeParameters());
        AssertThat(CurveWriter.Append(closed, Local(ShapeKind.Straight, new ShapeParameters())).Error).StartsWith("The path's curve is closed");
    }

    [TestCase]
    [RequireGodotRuntime]
    public void Appending_to_an_empty_curve_starts_at_the_default_place()
    {
        var result = CurveWriter.Append(new Curve3D(), Local(ShapeKind.Straight, new ShapeParameters { Length = 4 }));
        AssertThat(result.Note).IsNotNull();
        AssertThat(result.Curve.GetPointPosition(1)).IsEqual(new Vector3(4, 0, 0));
        var single = new Curve3D();
        single.AddPoint(new Vector3(0, 1, 0));
        var fromPoint = CurveWriter.Append(single, Local(ShapeKind.Straight, new ShapeParameters { Length = 4 }));
        AssertThat(fromPoint.Note).StartsWith("The curve's end has no direction");
        AssertThat(fromPoint.Curve.GetPointPosition(1)).IsEqual(new Vector3(4, 1, 0));
    }
}
