using System;
using Bencodex.Types;
using GraphQL;
using GraphQL.Types;
using Libplanet;
using Libplanet.Action;
using Libplanet.Explorer.GraphTypes;
using Nekoyume;
using Nekoyume.Action;
using Nekoyume.Model.Item;
using Nekoyume.Model.State;
using Nekoyume.TableData;
using NineChronicles.Headless.GraphTypes.States;
using NineChronicles.Headless.GraphTypes.States.Models.Item.Enum;
using NineChronicles.Headless.GraphTypes.States.Models.Table;

namespace NineChronicles.Headless.GraphTypes
{
    public class StateQuery : ObjectGraphType<(AccountStateGetter accountStateGetter, AccountBalanceGetter accountBalanceGetter)>
    {
        public StateQuery()
        {
            Name = "StateQuery";
            Field<AvatarStateType>(
                name: "avatar",
                description: "State for avatar.",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<AddressType>>
                {
                    Name = "avatarAddress",
                    Description = "Address of avatar."
                }),
                resolve: context =>
                {
                    var address = context.GetArgument<Address>("avatarAddress");
                    try
                    {
                        return context.Source.accountStateGetter.GetAvatarState(address);
                    }
                    catch (InvalidAddressException)
                    {
                        throw new InvalidOperationException($"The state {address} doesn't exists");
                    }
                });
            Field<RankingMapStateType>(
                name: "rankingMap",
                description: "State for avatar EXP record.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "index",
                        Description = "RankingMapState index. 0 ~ 99"
                    }),
                resolve: context =>
                {
                    var index = context.GetArgument<int>("index");
                    if (context.Source.accountStateGetter(RankingState.Derive(index)) is { } state)
                    {
                        return new RankingMapState((Dictionary) state);
                    }

                    return null;
                });
            Field<ShopStateType>(
                name: "shop",
                description: "State for shop.",
                deprecationReason: "Shop is migrated to ShardedShop and not using now. Use shardedShop() instead.",
                resolve: context => context.Source.accountStateGetter(Addresses.Shop) is { } state
                    ? new ShopState((Dictionary) state)
                    : null);
            Field<ShardedShopStateType>(
                name: "shardedShop",
                description: "State for sharded shop.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<ItemSubTypeEnumType>>
                    {
                        Name = "itemSubType",
                        Description = "ItemSubType for shard. see from https://github.com/planetarium/lib9c/blob/main/Lib9c/Model/Item/ItemType.cs#L13"
                    },
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "nonce",
                        Description = "Nonce for shard. It's not considered if itemSubtype is kind of costume or title. 0 ~ 15"
                    }),
                resolve: context =>
                {
                    var subType = context.GetArgument<ItemSubType>("itemSubType");
                    var nonce = context.GetArgument<int>("nonce").ToString("X").ToLower();

                    if (context.Source.accountStateGetter(ShardedShopState.DeriveAddress(subType, nonce)) is { } state)
                    {
                        return new ShardedShopState((Dictionary) state);
                    }

                    return null;
                });
            Field<WeeklyArenaStateType>(
                name: "weeklyArena",
                description: "State for weekly arena.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<IntGraphType>>
                    {
                        Name = "index",
                        Description = "WeeklyArenaState index. It increases every 56,000 blocks."
                    }),
                resolve: context =>
                {
                    var index = context.GetArgument<int>("index");
                    if (context.Source.accountStateGetter(WeeklyArenaState.DeriveAddress(index)) is { } state)
                    {
                        return new WeeklyArenaState((Dictionary) state);
                    }

                    return null;
                });
            Field<AgentStateType>(
                name: "agent",
                description: "State for agent.",
                arguments: new QueryArguments(new QueryArgument<NonNullGraphType<AddressType>>
                {
                    Name = "address",
                    Description = "Address of agent."
                }),
                resolve: context =>
                {
                    var address = context.GetArgument<Address>("address");
                    if (context.Source.accountStateGetter(address) is { } state)
                    {
                        return (new AgentState((Dictionary) state), context.Source.accountStateGetter, context.Source.accountBalanceGetter);
                    }

                    return null;
                }
            );

            Field<MonsterCollectionStateType>(
                nameof(MonsterCollectionState),
                description: "State for monster collection.",
                arguments: new QueryArguments(
                    new QueryArgument<NonNullGraphType<AddressType>>
                    {
                        Name = "agentAddress",
                        Description = "Address of agent."
                    }
                ),
                resolve: context =>
                {
                    var agentAddress = context.GetArgument<Address>("agentAddress");
                    if (!(context.Source.accountStateGetter(agentAddress) is Dictionary value))
                    {
                        return null;
                    }
                    var agentState = new AgentState(value);
                    var deriveAddress = MonsterCollectionState.DeriveAddress(agentAddress, agentState.MonsterCollectionRound);
                    if (context.Source.accountStateGetter(deriveAddress) is Dictionary state)
                    {
                        return new MonsterCollectionState(state);
                    }

                    return null;
                }
            );

            Field<MonsterCollectionSheetType>(
                nameof(MonsterCollectionSheet),
                resolve: context =>
                {
                    var sheetAddress = Addresses.GetSheetAddress<MonsterCollectionSheet>();
                    var rewardSheetAddress = Addresses.GetSheetAddress<MonsterCollectionRewardSheet>();
                    if (context.Source.accountStateGetter(sheetAddress) is { } ss &&
                        context.Source.accountStateGetter(rewardSheetAddress) is { } srs)
                    {
                        var monsterCollectionSheet = new MonsterCollectionSheet();
                        monsterCollectionSheet.Set((Text) ss);
                        var monsterCollectionRewardSheet = new MonsterCollectionRewardSheet();
                        monsterCollectionRewardSheet.Set((Text) srs);
                        return (monsterCollectionSheet, monsterCollectionRewardSheet);
                    }

                    return null;
                }
            );
        }
    }
}
