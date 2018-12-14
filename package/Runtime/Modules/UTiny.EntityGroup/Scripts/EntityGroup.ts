namespace ut {

    export class EntityGroup {

        /**
         * @method
         * @desc Creates a new instance of the given entity group by name and returns all entities
         * @param {ut.World} world - The world to add to
         * @param {string} name - The fully qualified name of the entity group 
         * @returns Flat list of all created entities
         */
        static instantiate(world: ut.World, name: string) : ut.Entity[] {
            let data = this.getEntityGroupData(name);

            if (data == undefined)
                throw "ut.EntityGroup.instantiate: No 'EntityGroup' was found with the name '" + name + "'";

            return data.load(world);
        };

        /**
         * @method
         * @desc Destroys all entities that were instantated with the given group name
         * @param {ut.World} world - The world to destroy from
         * @param {string} name - The fully qualified name of the entity group 
         */
        static destroyAll(world: ut.World, name: string) {

            let type = this.getEntityGroupData(name).Component;

            world.forEach([ut.Entity, type],
                (entity, instance) => {
                    // @TODO This should REALLY not be necessary
                    // We are protecting against duplicate calls to `destroyAllEntityGroups` within an iteration
                    if (world.exists(entity)) {
                      world.destroyEntity(entity);
                    }
                }
            );
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