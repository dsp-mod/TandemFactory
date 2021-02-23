using BepInEx;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TandemFactory
{
    [BepInPlugin("sky.plugins.dsp.TandemFactory", "TandemFactory", "1.0")]
    public class TandemFactory : BaseUnityPlugin
    {
		static int[] back;
        void Start()
        {
            Harmony.CreateAndPatchAll(typeof(TandemFactory), null);
        }
        [HarmonyPrefix]
        [HarmonyPatch(typeof(InserterComponent), "InternalUpdate")]
        public static bool InternalUpdate(InserterComponent __instance, PlanetFactory factory, int[][] needsPool, AnimData[] animPool, float power)
        {
			animPool[__instance.entityId].power = power;
			if (power < 0.1f)
			{
				return false;
			}
			switch (__instance.stage)
			{
				case EInserterStage.Picking:
					animPool[__instance.entityId].time = 0f;
					if (__instance.pickTarget == 0)
					{
						factory.entitySignPool[__instance.entityId].signType = 10U;
					}
					else if (__instance.insertTarget == 0)
					{
						factory.entitySignPool[__instance.entityId].signType = 10U;
					}
					else
					{
						if (__instance.itemId == 0)
						{
							int num = 0;
							int[] array = needsPool[__instance.insertTarget];
							if (__instance.careNeeds)
							{
								if (array != null)
								{
									num = factory.PickFrom(__instance.pickTarget, (int)__instance.pickOffset, __instance.filter, array);
								}
							}
							else
							{
								num = factory.PickFrom(__instance.pickTarget, (int)__instance.pickOffset, __instance.filter, array);
							}
							//此处新增,如果工厂未能捡起物品,判断爪子抓取端是否在一个工厂上,如果在,本工厂可以从抓取端工厂接收与本工厂产物相同的物品
							//暂未发现不良影响
							if(num<=0 && factory.entityPool[__instance.pickTarget].assemblerId > 0 && factory.entityPool[__instance.insertTarget].assemblerId > 0)
                            {
								var products = factory.factorySystem.assemblerPool[factory.entityPool[__instance.insertTarget].assemblerId].products;
                                if (products != null)
                                {
									int[] tempArray = { 0,0,0,0,0,0 };
									for (int i = 0; i < products.Length; i++)
									{
										tempArray[i] = products[i];
									}
									num = factory.PickFrom(__instance.pickTarget, (int)__instance.pickOffset, __instance.filter, tempArray);
								}
							}
							//////////////////////////////
							if (num > 0)
							{
								__instance.itemId = num;
								__instance.stackCount++;
								__instance.time = 0;
							}
						}
						else if (__instance.stackCount < __instance.stackSize)
						{
							int num2 = 0;
							int[] array2 = needsPool[__instance.insertTarget];
							if (__instance.careNeeds)
							{
								if (array2 != null)
								{
									num2 = factory.PickFrom(__instance.pickTarget, (int)__instance.pickOffset, __instance.itemId, array2);
								}
							}
							else
							{
								num2 = factory.PickFrom(__instance.pickTarget, (int)__instance.pickOffset, __instance.itemId, array2);
							}
							//此处新增,如果工厂未能捡起物品,判断爪子抓取端是否在一个工厂上,如果在,本工厂可以从抓取端工厂接收与本工厂产物相同的物品
							//暂未发现不良影响
							if (num2 <= 0 && factory.entityPool[__instance.pickTarget].assemblerId > 0 && factory.entityPool[__instance.insertTarget].assemblerId > 0)
							{
								var products = factory.factorySystem.assemblerPool[factory.entityPool[__instance.insertTarget].assemblerId].products;
								if (products != null)
								{
									int[] tempArray = { 0, 0, 0, 0, 0, 0 };
									for (int i = 0; i < products.Length; i++)
									{
										tempArray[i] = products[i];
									}
									num2 = factory.PickFrom(__instance.pickTarget, (int)__instance.pickOffset, __instance.filter, tempArray);
								}
							}
							//////////////////////////////
							if (num2 > 0)
							{
								__instance.stackCount++;
								__instance.time = 0;
							}
						}
						if (__instance.itemId > 0)
						{
							__instance.time += __instance.speed;
							if (__instance.stackCount == __instance.stackSize || __instance.time >= __instance.delay)
							{
								__instance.time = (int)(power * (float)__instance.speed);
								__instance.stage = EInserterStage.Sending;
							}
						}
						else
						{
							__instance.time = 0;
						}
					}
					break;
				case EInserterStage.Sending:
					__instance.time += (int)(power * (float)__instance.speed);
					if (__instance.time >= __instance.stt)
					{
						__instance.stage = EInserterStage.Inserting;
						__instance.time -= __instance.stt;
						animPool[__instance.entityId].time = 0.5f;
					}
					else
					{
						animPool[__instance.entityId].time = (float)__instance.time / ((float)__instance.stt * 2f);
					}
					if (__instance.itemId == 0)
					{
						__instance.stage = EInserterStage.Returning;
						__instance.time = __instance.stt - __instance.time;
					}
					break;
				case EInserterStage.Inserting:
					animPool[__instance.entityId].time = 0.5f;
					if (__instance.insertTarget == 0)
					{
						factory.entitySignPool[__instance.entityId].signType = 10U;
					}
					else if (__instance.itemId == 0 || __instance.stackCount == 0)
					{
						__instance.itemId = 0;
						__instance.stackCount = 0;
						__instance.time += (int)(power * (float)__instance.speed);
						__instance.stage = EInserterStage.Returning;
					}
					else if (factory.InsertInto(__instance.insertTarget, (int)__instance.insertOffset, __instance.itemId))
					{
						__instance.stackCount--;
						if (__instance.stackCount == 0)
						{
							__instance.itemId = 0;
							__instance.time += (int)(power * (float)__instance.speed);
							__instance.stage = EInserterStage.Returning;
						}
					}
					break;
				case EInserterStage.Returning:
					__instance.time += (int)(power * (float)__instance.speed);
					if (__instance.time >= __instance.stt)
					{
						__instance.stage = EInserterStage.Picking;
						__instance.time = 0;
						animPool[__instance.entityId].time = 0f;
					}
					else
					{
						animPool[__instance.entityId].time = (float)(__instance.time + __instance.stt) / ((float)__instance.stt * 2f);
					}
					break;
			}
			animPool[__instance.entityId].state = (uint)__instance.itemId;
			factory.factorySystem.inserterPool[__instance.id] = __instance;
			return false;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetFactory), "InsertInto")]
		public static bool InsertInto(ref bool __result,PlanetFactory __instance, int entityId, int offset, int itemId)
        {
			//爪子向工厂送货时,如果爪子内的物品是工厂的产物,工厂接受该产物
			//暂未发现不良影响
			int assemblerId = __instance.entityPool[entityId].assemblerId;
			if (assemblerId > 0)
			{
				int[] products = __instance.factorySystem.assemblerPool[assemblerId].products;
				int[] produced = __instance.factorySystem.assemblerPool[assemblerId].produced;
				if (products == null)
                {
					return true;
                }
				for (int i = 0; i < products.Length; i++)
				{
					if (products[i] == itemId)
					{
						produced[i]++;
						__result = true;
						return false;
					}
				}
				return true;
            }
            else
            {
				return true;
			}
        }
		[HarmonyPrefix]
		[HarmonyPatch(typeof(PlanetFactory), "PickFrom")]
		public static bool PickFrom(ref int __result, PlanetFactory __instance, int entityId, int offset, int filter, int[] needs)
		{
			if (needs != null && needs[0] == 0 && needs[1] == 0 && needs[2] == 0 && needs[3] == 0 && needs[4] == 0 && needs[5] == 0)
			{
				return true;
			}
			int assemblerId = __instance.entityPool[entityId].assemblerId;
			if (assemblerId > 0)
			{
				int[] array = __instance.factorySystem.assemblerPool[assemblerId].requires;
				int[] served = __instance.factorySystem.assemblerPool[assemblerId].served;
				if (array == null)
				{
					return true;
				}
				for (int i = 0; i < served.Length; i++)
				{
					if (served[i] > 0 && array[i] > 0 && (filter == 0 || filter == array[i]) && needs != null&&(needs[0] == array[i] || needs[1] == array[i] || needs[2] == array[i] || needs[3] == array[i] || needs[4] == array[i] || needs[5] == array[i]))
					{
						served[i]--;
						__result= array[i];
						return false;
					}
				}
			}
			return true;
		}
		[HarmonyPrefix]
		[HarmonyPatch(typeof(UIInserterBuildTip), "SetOutputEntity")]
		public static bool SetOutputEntityPre(UIInserterBuildTip __instance, int entityId)
        {
			
			PlanetFactory factory = Traverse.Create(__instance).Field("actionBuild").GetValue<PlayerAction_Build>().player.factory;
			EntityData entityData = factory.entityPool[entityId];
			if (entityData.assemblerId > 0 && factory.factorySystem.assemblerPool[entityData.assemblerId].recipeId!=0)
            {
				back = factory.factorySystem.assemblerPool[entityData.assemblerId].products;
				factory.factorySystem.assemblerPool[entityData.assemblerId].products = factory.factorySystem.assemblerPool[entityData.assemblerId].products.Concat(factory.factorySystem.assemblerPool[entityData.assemblerId].requires).ToArray();
			}
			return true;
		}
		[HarmonyPostfix]
		[HarmonyPatch(typeof(UIInserterBuildTip), "SetOutputEntity")]
		public static void SetOutputEntityPost(UIInserterBuildTip __instance, int entityId)
		{

			PlanetFactory factory = Traverse.Create(__instance).Field("actionBuild").GetValue<PlayerAction_Build>().player.factory;
			EntityData entityData = factory.entityPool[entityId];
			if (entityData.assemblerId > 0 && factory.factorySystem.assemblerPool[entityData.assemblerId].recipeId != 0)
			{
				factory.factorySystem.assemblerPool[entityData.assemblerId].products = back;
			}
		}
	}
}
