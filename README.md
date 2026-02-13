# Akka Cluster Sharding Repro

## Description

### Context

- akka shraded cluster
- statefull `ProcessActor` that:
      - is initialized by handling `StartCommands`; the `StartCommand` sets the Id that will be used by all subsequent `HitCommands` that need to land in the same actor
      - updates it's state by handling `HitCommands`
- the `ProcessMessageExtractor` is used to route the commands to the correct actor instance

### Issue

In the `ProcessMessageExtractor` in the case of the `StartCommand`:
- ✅ if the EntityId is taken out of the `StartCommand` all works as expected
- ❌ if the EntityId is genrated in the `ProcessMessageExtractor`, the `StartCommand` will be routed to an actor instance but any subsequent `HitCommands` will be routed to another actor instance that has the same id but is created on a different shard. 

## Repro Steps

- solution is build on dotnet 8 + akkaAkka.Cluster.Hosting v1.5.60
- open [./AkkaShardingSandbox.sln](./AkkaShardingSandbox.sln)
- set `USE_ID_FROM_COMMAND` in [Program.cs](./AkkaShardingSandbox/Program.cs) on line 12
      - to `false` to set the Id of the `StartCommand` inside the `ProcessMessageExtractor`
      - to `true` to use the existing Id from the `StartCommand`
- run project

## Expected behaviour when `USE_ID_FROM_COMMAND = true`(the `ProcessMessageExtractor` takes the id from the `StartCommand`)

``` bash
info: AkkaShardingSandbox.ProcessActor[0]
      USE_ID_FROM_COMMAND is True
#.... logs redacted for brevity
# ...
# ...

# initial StartCommand lands in /ProcessActor/5/a4b214923a604e7f8732d98b2cd10a36
info: AkkaShardingSandbox.ProcessActor[0]
      START in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/5/a4b214923a604e7f8732d98b2cd10a36
info: AkkaShardingSandbox.ProcessActor[0]
      started process a4b214923a604e7f8732d98b2cd10a36
# subsequent HitCommands land in the same /ProcessActor/5/a4b214923a604e7f8732d98b2cd10a36
# and increment the state as expected
info: AkkaShardingSandbox.ProcessActor[0]
      HIT in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/5/a4b214923a604e7f8732d98b2cd10a36
info: AkkaShardingSandbox.ProcessActor[0]
      hit 1 on process a4b214923a604e7f8732d98b2cd10a36 => 1
info: AkkaShardingSandbox.ProcessActor[0]
      HIT in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/5/a4b214923a604e7f8732d98b2cd10a36
info: AkkaShardingSandbox.ProcessActor[0]
      hit 2 on process a4b214923a604e7f8732d98b2cd10a36 => 2
info: AkkaShardingSandbox.ProcessActor[0]
      HIT in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/5/a4b214923a604e7f8732d98b2cd10a36
info: AkkaShardingSandbox.ProcessActor[0]
      hit 3 on process a4b214923a604e7f8732d98b2cd10a36 => 3
info: AkkaShardingSandbox.ProcessActor[0]
      HIT in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/5/a4b214923a604e7f8732d98b2cd10a36
info: AkkaShardingSandbox.ProcessActor[0]
      hit 4 on process a4b214923a604e7f8732d98b2cd10a36 => 4
info: AkkaShardingSandbox.ProcessActor[0]
      HIT in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/5/a4b214923a604e7f8732d98b2cd10a36
info: AkkaShardingSandbox.ProcessActor[0]
      hit 5 on process a4b214923a604e7f8732d98b2cd10a36 => 5

```

## Actual behaviour when `USE_ID_FROM_COMMAND = false`(the Id is generated inside the `ProcessMessageExtractor`)

``` bash
info: AkkaShardingSandbox.ProcessActor[0]
      USE_ID_FROM_COMMAND is False
#.... logs redacted for brevity
# ...
# ...

# initial StartCommand lands in /ProcessActor/8/4c248edcc61e4c549488e0600eae9c51
info: AkkaShardingSandbox.ProcessActor[0]
      START in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/8/4c248edcc61e4c549488e0600eae9c51
info: AkkaShardingSandbox.ProcessActor[0]
      started process 4c248edcc61e4c549488e0600eae9c51

# susbsequent HitCommands land in /ProcessActor/7/4c248edcc61e4c549488e0600eae9c51 which is initialized
# actor has same id, but different Shardid (7 vs 8 on Start)
warn: AkkaShardingSandbox.ProcessActor[0]
      uninitialized HIT in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/7/4c248edcc61e4c549488e0600eae9c51
info: AkkaShardingSandbox.ProcessActor[0]
      hit 1 on process 4c248edcc61e4c549488e0600eae9c51 => -1
warn: AkkaShardingSandbox.ProcessActor[0]
      uninitialized HIT in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/7/4c248edcc61e4c549488e0600eae9c51
info: AkkaShardingSandbox.ProcessActor[0]
      hit 2 on process 4c248edcc61e4c549488e0600eae9c51 => -1
warn: AkkaShardingSandbox.ProcessActor[0]
      uninitialized HIT in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/7/4c248edcc61e4c549488e0600eae9c51
info: AkkaShardingSandbox.ProcessActor[0]
      hit 3 on process 4c248edcc61e4c549488e0600eae9c51 => -1
warn: AkkaShardingSandbox.ProcessActor[0]
      uninitialized HIT in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/7/4c248edcc61e4c549488e0600eae9c51
info: AkkaShardingSandbox.ProcessActor[0]
      hit 4 on process 4c248edcc61e4c549488e0600eae9c51 => -1
warn: AkkaShardingSandbox.ProcessActor[0]
      uninitialized HIT in akka://AkkaShardingSandboxSystem/system/sharding/ProcessActor/7/4c248edcc61e4c549488e0600eae9c51
info: AkkaShardingSandbox.ProcessActor[0]
      hit 5 on process 4c248edcc61e4c549488e0600eae9c51 => -1

```