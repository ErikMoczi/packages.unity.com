namespace ut
{
  export class EntityLookupCache
  {
    static _cache : {[key: string]: ut.Entity} = {}

    static getByName(world: ut.World,name: string) : ut.Entity {
      let entity : ut.Entity;
      
      if (name in this._cache) {
        entity = this._cache[name];
        if (world.exists(entity)) return entity;
      }

      entity = world.getEntityByName(name);
      this._cache[name] = entity;
      return entity;
    }
  }
}