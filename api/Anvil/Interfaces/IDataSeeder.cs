namespace Anvil.Interfaces;

/// <summary>
/// Seeds development or CI data for the hosting product.
/// </summary>
/// <remarks>
/// The shared layer owns when seeding runs — environment gating, the SeedEnabled
/// configuration flag, scope creation, and failure isolation. The product owns what
/// is written, by registering its own implementation of this interface.
/// </remarks>
public interface IDataSeeder
{
    /// <summary>
    /// Writes seed data. Implementations MUST be idempotent.
    /// </summary>
    void Seed();
}
