using Xunit;

// The DB-backed suites each spin up their own connection pool. Hosted Postgres poolers
// cap simultaneous clients (Supabase session mode: 15), so running test classes in
// parallel exhausts it (EMAXCONNSESSION). Sequential classes keep the total low.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
