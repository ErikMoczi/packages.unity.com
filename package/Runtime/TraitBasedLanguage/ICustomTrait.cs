using Unity.Properties;


namespace Unity.AI.Planner.DomainLanguage.TraitBased
{
    /// <summary>
    /// Interface marking a trait as a custom implementation. Base interface for <see cref="ICustomTrait{T}"/>.
    /// </summary>
    public interface ICustomTrait : ITrait
    {
    }

    /// <summary>
    /// Interface marking a trait as a custom implementation
    /// </summary>
    /// <typeparam name="T">Trait type</typeparam>
    public interface ICustomTrait<T> : ICustomTrait, ITrait<T> where T : struct, IPropertyContainer
    {
    }
}
