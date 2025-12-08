/*
 * ==============================================================================
 * Filename: BattleField
 * Created:  2025 / 9 / 25
 * Author: HuaHua
 * Purpose: 战场
 * ==============================================================================
 **/

using Binary;
using Framework.Core;
using Framework.Core.Fixed;
using mathematics;
using MemoryCopy;
using System.Collections.Generic;
using System.Collections.Generic.Open;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Battle
{
    [Binary, MemoryCopy(ENotFullCopyType.NeedCopy)]
    public partial class BattleField : IDataPool, IPtrEntity<BattleField>
    {
        public PtrEntity<BattleField> Ptr
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }
        public EntityPool EPool
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }
        public uint PtrGUID
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }
        public MemoryPool MPool
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set;
        }

        #region 数据

        [BinaryInore, MemoryCopyMemberReference]
        public BFSetting Setting; // 战斗配置

        private PtrEntity<QuadTreeSystem> _ptrQuadTree;               // 场景单位四叉树管理
        private PtrEntity<SceneSystem> _ptrSceneSystem;
        private PtrEntity<PointSystem> _ptrPointSystem;                     // 场景点位管理器
        private PtrEntity<GridSystem> PtrGridSystem;
        private PtrEntity<VisibleCellDataSystem> _ptrVisibleSystem;     //迷雾的视野数据
        private PtrEntity<BulletManager> PtrBulletMgr;                // 战场中所有的子弹管理器
        private PtrEntity<EffectSystem> PtrEffectSystem;              // 特效管理器
        private PtrEntity<ActorSkillDestroyManager> PtrActorSkillDestroyMgr;           // 处理Actor技能延时销毁
        private PtrEntity<CommonReactableObjectManager> PtrCommonReactObjMgr; // 交互管理器

        /// <summary>
        /// key is  ECampType
        /// </summary>
        private OpenDictionary_int<PtrEntity<Team>> _ptrTeams;              //队伍    
        private OpenList<PtrEntity<Actor>> _ptrActorsList;                  // 所有有效的单位对象（遍历用）
        private OpenDictionary_int<PtrEntity<Actor>> _ptrActors;            // 所有有效的单位对象（索引用）
        private OpenDictionary<uint, PtrEntity<Actor>> _ptrGuidActors;            // 所有有效的单位对象（使用GUID索引用）
        private OpenDictionary_int<PtrEntity<Actor>> _ptrServerActors;      // 服务器索引Actor字典


        [BinaryInore, MemoryCopyMemberIgnore]
        private PtrEntityCacheDictionary<int, Actor> _actorsCache = new(false, 8);
        [BinaryInore, MemoryCopyMemberIgnore]
        private PtrEntityCacheDictionary<uint, Actor> _actorsGuidCache = new(false, 8);
        [BinaryInore, MemoryCopyMemberIgnore]
        private PtrEntityCacheList<Actor> _actorsCacheList = new(false, 8);
        /// <summary>
        /// 战场数据
        /// </summary>
        private BattleFieldData _data;
        public ref BattleFieldData Data => ref _data;

        public ref int LogicFrameIndex => ref this._data.LogicFrameIndex;            // 逻辑帧序号(当前的)
        public ref int LogicFrameCount => ref this._data.LogicFrameCount;            // 逻辑帧数
        public ref ffloat LogicFrameDeltaTime => ref this._data.LogicFrameDeltaTime; // 逻辑帧间隔(秒)
        public ref ffloat LogicFrameDeltaTimeMS => ref this._data.LogicFrameDeltaTimeMs; // 逻辑帧间隔(毫秒)
        public ref ffloat LogicFrameTime => ref this._data.LogicFrameTime;           // 逻辑帧时间(当前的)
        public ref FRandom RandomGenerator => ref this.Data.RandomGenerator;


        public OpenDictionary_int<Actor> AllActors => this._actorsCache.GetOpenDictionaryInt(this._ptrActors, this.EPool);
        public Dictionary<uint, Actor> AllActorsByGuid => this._actorsGuidCache.Get(this._ptrGuidActors, this.EPool);
        public OpenList<Actor> AllActorsList => this._actorsCacheList.GetOpenList(this._ptrActorsList, this.EPool);


        [BinaryInore, MemoryCopyMemberCopySetDefault]
        private PointSystem _pointSystem;
        public PointSystem PointSystem // 场景点位管理器
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => this._pointSystem ??= this._ptrPointSystem.Get(this.EPool);
        }

        [BinaryInore, MemoryCopyMemberCopySetDefault]
        private VisibleCellDataSystem _visibleSystem;
        public VisibleCellDataSystem VisibleSystem
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => this._visibleSystem ??= this._ptrVisibleSystem.Get(this.EPool);
        }


        [BinaryInore, MemoryCopyMemberIgnore]
        private ScriptNodeCreator _scriptCreator; // 脚本节点创建管理器
        public ScriptNodeCreator ScriptCreator
        {
            get
            {
                if (this._scriptCreator != null)
                    return this._scriptCreator;
                this._scriptCreator = new ScriptNodeCreator();
                this._scriptCreator.Initialize();
                return this._scriptCreator;
            }
        }

        [BinaryInore, MemoryCopyMemberIgnore]
        private EventSystem _eventSystem;
        public EventSystem EventSystem => _eventSystem;


        [BinaryInore, MemoryCopyMemberCopySetDefault]
        private QuadTreeSystem _quadTreeSystem;
        public QuadTreeSystem QuadTreeSystem // 场景单位四叉树管理
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._quadTreeSystem ??= this._ptrQuadTree.Get(this.EPool);
        }

        [BinaryInore, MemoryCopyMemberCopySetDefault]
        private SceneSystem _sceneSystem;
        public SceneSystem SceneSystem
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => this._sceneSystem ??= this._ptrSceneSystem.Get(this.EPool);
        }
        

        [BinaryInore, MemoryCopyMemberCopySetDefault]
        private GridSystem _gridSystem;
        public GridSystem GridSystem // 网格管理器
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => this._gridSystem ??= this.PtrGridSystem.Get(this.EPool);
        }

        [BinaryInore, MemoryCopyMemberCopySetDefault]
        private BulletManager mBulletMgr;
        public BulletManager BulletMgr // 战场中所有的子弹管理器
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => this.mBulletMgr ??= this.PtrBulletMgr.Get(this.EPool);
        }

        [BinaryInore, MemoryCopyMemberCopySetDefault]
        private EffectSystem mEffectSystem;
        public EffectSystem EffectSystem // 特效管理器
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => this.mEffectSystem ??= this.PtrEffectSystem.Get(this.EPool);
        }

        [BinaryInore, MemoryCopyMemberCopySetDefault]
        private CommonReactableObjectManager mCommonReactObjMgr;
        public CommonReactableObjectManager CommonReactObjMgr // 交互管理器
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => this.mCommonReactObjMgr ??= this.PtrCommonReactObjMgr.Get(this.EPool);
        }

        [BinaryInore, MemoryCopyMemberIgnore]
        private BattleRenderEventDistributeManager mBattleRenderEvent;
        public BattleRenderEventDistributeManager BattleRenderEvent
        {
            get
            {
                if (this.mBattleRenderEvent == null)
                {
                    this.mBattleRenderEvent = new BattleRenderEventDistributeManager();
                    this.mBattleRenderEvent.Initialize();
                }
                return this.mBattleRenderEvent;
            }
        }

        [BinaryInore, MemoryCopyMemberCopySetDefault]
        private ActorSkillDestroyManager mActorSkillDestroyMgr;
        public ActorSkillDestroyManager ActorSkillDestroyMgr // 处理Actor技能延时销毁
        {
            [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining)]
            get => this.mActorSkillDestroyMgr ??= this.PtrActorSkillDestroyMgr.Get(this.EPool);
        }

        #endregion

        /// <summary>
        /// 初始化英雄出生点
        /// </summary>
        private void OnInitializeHeroBornPoint()
        {
            var pointSettings = this.Setting.PointSetting;


        }


        /// <summary>
        /// 
        /// </summary>
        public void Initialize(BattleInitData parameter, EventSystem eventSystem)
        {
            this._eventSystem = eventSystem;
            this._ptrActorsList = new();
            this._ptrActors = new();
            this._ptrGuidActors = new();
            this._ptrServerActors = new();

            ref var data = ref this._data;
            data.LogicFrameCount = BattleInitData.LogicFrameCount;
            data.LogicFrameDeltaTime = ffloat.MakeFixNum(1, data.LogicFrameCount) + ffloat.Precision;
            data.LogicFrameDeltaTimeMs = data.LogicFrameDeltaTime * ffloat.Thousand;

            data.TimeScale = ffloat.One;
            data.LogicFrameIndex = -1;
            data.LogicFrameTime = ffloat.Zero;
            data.RandomSeed = parameter.RandomSeed;
            data.MainHeroServerID = parameter.MainHeroServerID;
            
            //四叉树
            var maxS = fmath.max(parameter.SceneBlockData.Width, parameter.SceneBlockData.Height);
            this._quadTreeSystem = this.EPool.Alloc(out this._ptrQuadTree);
            this._quadTreeSystem.Initialize(ffloat.Zero, maxS);
            
            // 静态阻挡
            this._sceneSystem = this.EPool.Alloc(out this._ptrSceneSystem);
            this._sceneSystem.Initialize(parameter, this);

            // 场景点位管理
            this._pointSystem = this.EPool.Alloc(out this._ptrPointSystem);
            this._pointSystem.Initialize(this);

            // 初始化网格管理 
            this._gridSystem = this.EPool.Alloc(out this.PtrGridSystem);
            this._gridSystem.Initialize(this, parameter);

            // 初始化迷雾数据
            this._visibleSystem = this.EPool.Alloc(out this._ptrVisibleSystem);
            this._visibleSystem.Initialize(parameter.MapVisibleCellData, parameter.MapVisibleWidth, parameter.MapVisibleHeight, parameter.MapVisibleRadius);

            // 初始化子弹数据
            this.mBulletMgr = this.EPool.Alloc(out this.PtrBulletMgr);
            this.mBulletMgr.Initialize(this);

            // 初始化特效系统
            this.mEffectSystem = this.EPool.Alloc(out this.PtrEffectSystem);
            this.mEffectSystem.Initialize(this);

            // 初始化React管理
            this.mCommonReactObjMgr = this.EPool.Alloc(out this.PtrCommonReactObjMgr);
            this.mCommonReactObjMgr.Initialize(this, this._gridSystem);


            this.mActorSkillDestroyMgr = this.EPool.Alloc(out this.PtrActorSkillDestroyMgr);
            
            // 队伍
            this._ptrTeams = new OpenDictionary_int<PtrEntity<Team>>();
            for (var camp = Setting.BattleSetting.MinCampType; camp <= Setting.BattleSetting.MaxCampType; ++camp)
            {
                var team = this.EPool.Alloc<Team>(out var ptrTeam);
                team.Initialize(this, parameter.Teams[(int)camp]);
                this._ptrTeams.Add((int)camp, ptrTeam);
            }
            this.InitializeGameFlow();
            //初始化英雄出生点
            this.OnInitializeHeroBornPoint();

#if UNITY_EDITOR || DEBUG || DEVELOPMENT_BUILD
            InitCommandInterpreterProxy();
#endif
        }

        /// <summary>
        /// 战斗开始
        /// </summary>
        public void Start()
        {
            StartGameFlow();
#if UNITY_EDITOR || DEBUG || DEVELOPMENT_BUILD
            commandInterpreterProxy.Start();
#endif
        }

        /// <summary>
        /// 战斗结束
        /// </summary>
        public void End()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        public void Destroy()
        {
            this.PtrGridSystem.Free(this.EPool);
            this._gridSystem = default;
            this.PtrBulletMgr.Free(this.EPool);
            this.mBulletMgr = default;
            this.PtrEffectSystem.Free(this.EPool);
            this.mEffectSystem = default;
            this.PtrActorSkillDestroyMgr.Free(this.EPool);
            this.mActorSkillDestroyMgr = default;
            
            this._ptrSceneSystem.Free(this.EPool);
            this._ptrSceneSystem = default;
#if UNITY_EDITOR || DEBUG || DEVELOPMENT_BUILD
            LogManager.Log( "[CommandInterpreterProxy] Stopping...");
            commandInterpreterProxy.Stop();
#endif
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="frameIndex"></param>
        /// <param name="keyFrameList"></param>
        /// <returns></returns>
        public bool FrameUpdate(int frameIndex, List<KeyFrameCmd> keyFrameList)
        {
            this.LogicFrameTime += this.LogicFrameDeltaTime;
            var nLogicTicks = System.DateTime.Now.Ticks;
            var fDeltaTime = this.LogicFrameDeltaTime;
            var fDeltaTimeMS = this.LogicFrameDeltaTimeMS;
            var fFrameTime = this.LogicFrameTime;
            this._data.LogicFrameIndex = frameIndex;

            var allActorsList = AllActorsList;

            foreach (var keyFrameCmd in keyFrameList)
            {
                switch (keyFrameCmd.Cmd)
                {
                    case EBattleCmd.Move:
                        {
                            //var moveCmd = (MoveKeyFrameCmd)keyFrameCmd;
                            var position = keyFrameCmd.SendIndex;


                            for (int i = 0; i < allActorsList.Count; i++)
                            {
                                var actor = allActorsList[i];
                                if (actor.PositionIndex != position)
                                    continue;
                                actor.SetKeyFrameCacheSpaceCache(EBattleCmd.Move, keyFrameCmd);
                            }
                        }
                        break;
                    case EBattleCmd.DirectionSkill:
                        {
                            var skillCmd = (DirectionSkillKeyFrameCmd)keyFrameCmd;
                            var position = skillCmd.SendIndex;


                            for (int i = 0; i < allActorsList.Count; i++)
                            {
                                var actor = allActorsList[i];
                                if (actor.PositionIndex != position)
                                    continue;
                                actor.DoSkill(skillCmd);
                            }
                        }
                        break;
                    case EBattleCmd.NormalBtn:
                        {
                            //var normalCmd = (NormalButtonKeyFrameCmd)keyFrameCmd;
                            var position = keyFrameCmd.SendIndex;

                            for (int i = 0; i < allActorsList.Count; i++)
                            {
                                var actor = allActorsList[i];
                                if (actor.PositionIndex != position)
                                    continue;
                                actor.DoNormalBtnCmd((NormalButtonKeyFrameCmd)keyFrameCmd);
                            }
                            break;
                        }
                }
            }
            for (var camp = Setting.BattleSetting.MinCampType; camp <= Setting.BattleSetting.MaxCampType; ++camp)
            {
                this._ptrTeams[(int)camp].Get(this.EPool).FrameUpdate(fDeltaTime, fDeltaTimeMS);
            }


            allActorsList = AllActorsList;

            // 更新所有单位的渲染数据
            for (int i = 0; i < allActorsList.Count; i++)
            {
                var actor = allActorsList[i];
                actor.UpdateRender();

                // // 更新召唤物AI
                // if (actor.ActorType == EActorType.Summoner)
                // {
                //     actor.UpdateSummonerAI(fDeltaTime);
                // }
            }
            
           this.BulletMgr.FrameUpdate(fDeltaTime);
           
           this.EffectSystem.FrameUpdate(this);
           this.CommonReactObjMgr.FrameUpdate(fDeltaTime);
#if UNITY_EDITOR || DEBUG || DEVELOPMENT_BUILD
            try
            {
                commandInterpreterProxy.ProcessPendingCommands(frameIndex);
            }
            catch (System.Exception ex)
            {
                LogManager.LogError($"[CommandInterpreterProxy] Exception: {ex}");
            }
#endif
            return true;
        }

        /// <summary>
        /// 生成一个单位索引，用于创建新的单位
        /// 索引范围为[1, int.MaxValue]
        /// </summary>
        public int GenerateActorIndex()
        {
            if (this.Data.GenerateActorIndex < 1)
            {
                // 这里可以产生异常，如创建单位数量超过了int.MaxValue则单位索引可能重复
                // 防止产生负数索引 负数索引在某些地方会有特殊含义
                // 例如负数的阵营索引与0被用于buff归属判断
                this.Data.GenerateActorIndex = 1;
            }
            return this.Data.GenerateActorIndex++;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="actor"></param>
        public void AddActor(Actor actor)
        {
            if (actor == null)
                return;
            this._ptrActorsList.Add(actor.Ptr);
            this._ptrActors.Add(actor.ActorIndex, actor.Ptr);
            this._ptrGuidActors.Add(actor.PtrGUID, actor.Ptr);

            this._actorsCache.Dirty();
            this._actorsCacheList.Dirty();
            this._actorsGuidCache.Dirty();

            if (actor.ServerID != -1)
            {
                this._ptrServerActors.Add(actor.ServerID, actor.Ptr);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="actor"></param>
        public void RemoveActor(Actor actor)
        {
            if (actor == null)
                return;
            this._ptrActorsList.Remove(actor.Ptr);
            this._ptrActors.Remove(actor.ActorIndex);
            this._ptrGuidActors.Remove(actor.PtrGUID);
            this._actorsCache.Dirty();
            this._actorsCacheList.Dirty();
            this._actorsGuidCache.Dirty();

            if (actor.ServerID != -1)
            {
                this._ptrServerActors.Remove(actor.ServerID);
            }
        }

        /// <summary>
        /// 通过ActorIndex获取Actor
        /// </summary>
        /// <param name="actorIndex"></param>
        /// <returns></returns>
        public Actor GetActor(int actorIndex)
        {
            return AllActors.GetValueOrDefault(actorIndex);
        }

        /// <summary>
        /// 通过ServerID获取Actor
        /// </summary>
        /// <param name="serverID"></param>
        /// <returns></returns>
        public Actor GetActorByServerID(int serverID)
        {
            this._ptrServerActors.TryGetValue(serverID, out var ptrActor);
            return ptrActor.Get(this.EPool);
        }
        /// <summary>
        /// 通过guid获取Actor
        /// </summary>
        /// <param name="guid"></param>
        /// <returns></returns>
        public Actor GetActorByGuid(uint guid)
        {
            return AllActorsByGuid.GetValueOrDefault(guid);
        }
        public bool IsActorEscape(int positionIndex)
        {
            foreach (var surviorResult in mSurvivorResults)
            {
                if (surviorResult.PositionIndex == positionIndex)
                {
                    return surviorResult.IsEscaped;
                }
            }
            return false;
        }
        public bool IsActorExecuted(int positionIndex)
        { 
            foreach (var surviorResult in mSurvivorResults)
            {
                if (surviorResult.PositionIndex == positionIndex)
                {
                    return surviorResult.IsExecuted;
                }
            }
            return false;
        }
        public Team GetTeam(ECampType campType)
        {
            this._ptrTeams.TryGetValue((int)campType, out var ptrTeam);
            return ptrTeam.Get(this.EPool);
        }




#if UNITY_EDITOR || DEBUG || DEVELOPMENT_BUILD
        [BinaryInore, MemoryCopyMemberIgnore]
        private EventFramework.CommandInterpreterProxy commandInterpreterProxy;

        void InitCommandInterpreterProxy()
        {
            commandInterpreterProxy = new EventFramework.CommandInterpreterProxy();
            commandInterpreterProxy.LogHandler = (s) => LogManager.Log(s);
            commandInterpreterProxy.ErrorHandler = (s) => LogManager.LogError(s);

            commandInterpreterProxy.RegisterPresetVariable("#p", () => this.GetActorByServerID(this.Data.MainHeroServerID));
            commandInterpreterProxy.RegisterPresetVariable("#h0", () => this.GetTeam(ECampType.CtCamp0).GetHeroByIndex(0));
            commandInterpreterProxy.RegisterPresetVariable("#h1", () => this.GetTeam(ECampType.CtCamp0).GetHeroByIndex(1));
            commandInterpreterProxy.RegisterPresetVariable("#r0", () => this.GetTeam(ECampType.CtCamp1).GetHeroByIndex(0));
            commandInterpreterProxy.RegisterPresetVariable("#r1", () => this.GetTeam(ECampType.CtCamp1).GetHeroByIndex(1));
            commandInterpreterProxy.RegisterPresetVariable("#r2", () => this.GetTeam(ECampType.CtCamp1).GetHeroByIndex(2));
            commandInterpreterProxy.RegisterPresetVariable("#r3", () => this.GetTeam(ECampType.CtCamp1).GetHeroByIndex(3));
            commandInterpreterProxy.RegisterPresetVariable("#r4", () => this.GetTeam(ECampType.CtCamp1).GetHeroByIndex(4));

        }
#endif
    }
}