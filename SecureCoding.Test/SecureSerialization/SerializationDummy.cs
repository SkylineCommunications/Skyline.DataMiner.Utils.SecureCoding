namespace SecureCoding.Test.SecureSerialization.SerializationDummy
{
    public class SimpleDummy : IDummy
    {
        public readonly string HardcodedProperty = "hardcoded";
        public readonly string RuntimeProperty;

        private readonly int secret = 42;

        public SimpleDummy(string runtimeProperty)
        {
            RuntimeProperty = runtimeProperty;
        }

        public string Content { get; set; }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            if (obj is not SimpleDummy dummy)
            {
                return false;
            }

            return (this.HardcodedProperty == dummy.HardcodedProperty &&
                    this.RuntimeProperty == dummy.RuntimeProperty &&
                    this.secret == dummy.secret);
        }

        public override int GetHashCode()
        {
            return (HardcodedProperty.GetHashCode() + RuntimeProperty.GetHashCode() + secret) % 524287;
        }
    }

    public class ComplexDummy : IDummy
    {
        public List<IDummy> Dummies { get; set; }

        public Dictionary<ComplexDummy, IDummy> ComplexDummyToDummy { get; }

        public ComplexDummy()
        {

        }

        public ComplexDummy(List<IDummy> dummies)
        {
            Dummies = dummies;
        }

        public ComplexDummy(Dictionary<ComplexDummy, IDummy> dummies)
        {
            ComplexDummyToDummy = dummies;
        }

        public override bool Equals(object? obj)
        {
            if (obj == null) return false;
            if (obj == this) return true;
            if (obj is not ComplexDummy dummy)
            {
                return false;
            }

            if (this.Dummies != null && dummy.Dummies != null)
            {
                if (this.Dummies.Count != dummy.Dummies.Count) return false;

                for (int i = 0; i < this.Dummies.Count; i++)
                {
                    if (!this.Dummies[i].Equals(dummy.Dummies[i]))
                    {
                        return false;
                    }
                }
            }

            if (this.ComplexDummyToDummy != null && dummy.ComplexDummyToDummy != null)
            {
                if (this.ComplexDummyToDummy.Count != dummy.ComplexDummyToDummy.Count) return false;

                foreach (var kvp in ComplexDummyToDummy)
                {
                    if (!dummy.ComplexDummyToDummy.TryGetValue(kvp.Key, out var value))
                    {
                        return false;
                    }

                    if (value != kvp.Value)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 97;

            if (Dummies != null)
            {
                foreach (var dummy in Dummies)
                {
                    hash += dummy.GetHashCode();
                }
            }

            if (ComplexDummyToDummy != null)
            {
                foreach (var kvp in ComplexDummyToDummy)
                {
                    hash += kvp.GetHashCode();
                }
            }

            return hash % 39916801;
        }
    }

    public class IncompatibleDummy
    {
        public string Name = "Incompatible";
        public string Description = "I won't polymorph";
    }

    public interface IDummy { }
}
