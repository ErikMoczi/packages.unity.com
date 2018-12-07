namespace ut
{
  export class EntityLookupCache
  {
    static _cache = {}
    static _world;

    static initialize(world: ut.World) {
      this._world = world;
    }

    static exists(name: string): boolean {
      return name in this._cache;
    }

    static getByName(name: string) : ut.Entity {
      if (!this.exists(name)) {
        this._cache[name] = this._world.getEntityByName(name);
      }

      return this._cache[name];
    }
  }
}