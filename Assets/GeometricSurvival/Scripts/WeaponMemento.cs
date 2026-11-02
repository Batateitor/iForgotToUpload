namespace GeometricSurvival
{
    // Memento: immutable snapshot owned and interpreted by WeaponController.
    // Commands keep the snapshot without reading or modifying its contents.
    public sealed class WeaponMemento
    {
        internal int Selected { get; }
        internal WeaponController Owner { get; }
        internal WeaponMemento(WeaponController owner, int selected)
        { Owner = owner; Selected = selected; }
    }
}
