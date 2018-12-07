namespace ut.Streaming {
    /**
     * DO NOT RETAIN THIS OBJECT
     * Return structure for instantiate call
     * @note this is a temporary structure with deferred entities
     */
    export class EntityGroupInstantiationHandle {

        /**
         * Entity group meta information. 
         * This entity stores a single component of type ut.Streaming.EntityGroupInstance
         * @note This entity is deferred
         */
        entity: ut.Entity;

        /**
         * Handle to the spawned entities for this group
         */
        instance: ut.Streaming.EntityGroupInstance;

        /**
         * Returns the first entity that was spawned in the group 
         * @note This entity IS buffered
         */
        getRootEntity(): ut.Entity {
            return this.instance.entities[0];
        }

        constructor(entity: ut.Entity, instance: ut.Streaming.EntityGroupInstance) {
            this.entity = entity;
            this.instance = instance;
        }
    }

    export class StreamingService {

        /**
         * Creates a new instance of the given entity group by name using and existing command buffer
         *
         * @returns EntityGroupInstantiationHandle for the newly spawned group
         */
        static instantiate(world: ut.World, name: string): ut.Streaming.EntityGroupInstantiationHandle {
            let data = StreamingService.getEntityGroupData(name);

            if (data == undefined)
                throw "ut.Streaming.StreamingService.instantiate: No 'EntityGroup' was found with the name '" + name + "'";

            var entities = data.load(world);

            var instance = new ut.Streaming.EntityGroupInstance();
            instance.name = name;
            instance.entities = entities;

            // spawn an instance handle
            let handle = world.createEntity();
            world.addComponentData(handle, instance);

            return new EntityGroupInstantiationHandle(handle, instance);
        };

        /**
         * Destroys all entities that were instantated with the given group name using and existing command buffer
         */
        static destroyAllByName(world: ut.World, name: string) {
            world.forEach([ut.Entity, ut.Streaming.EntityGroupInstance],
                (entity, instance) => {
                    if (instance.name == name) {
                        StreamingService.internal_destroyEntityGroupInstance(world, entity, instance);
                    }
                }
            );
        }

        /**
         * Destroys the given entity group handle with an existing buffer
         *
         * The given entity should be the original handle created by `instantiate` NOT an entitiy from withing the group
         */
        static destroy(world: ut.World, entity: ut.Entity) {
            let instance = world.getComponentData(entity, ut.Streaming.EntityGroupInstance);
            StreamingService.internal_destroyEntityGroupInstance(world, entity, instance);
        }

        static internal_destroyEntityGroupInstance(world: ut.World, entity: ut.Entity, instance: ut.Streaming.EntityGroupInstance) {
            for (let i = 0; i < instance.entities.length; i++) {
                if (world.exists(instance.entities[i]))
                    world.destroyEntity(instance.entities[i]);
            }

            world.destroyEntity(entity);
        }

        /**
         * @method
         * @desc Returns an entity group object by name
         * @param {string} name - Fully qualified group name
         */
        static getEntityGroupData(name: string): ut.EntityGroupData {
            let parts = name.split('.');
            if (parts.length < 2)
                throw "ut.Streaming.StreamingService.getEntityGroupData: name entry is invalid";

            let shiftedParts = parts.shift();
            let initialData = entities[shiftedParts!];
            if (initialData == undefined)
                throw "ut.Streaming.StreamingService.getEntityGroupData: name entry is invalid";

            return parts.reduce(function (v: any, p: string) {
                return v[p];
            }, initialData);
        }
    }
}