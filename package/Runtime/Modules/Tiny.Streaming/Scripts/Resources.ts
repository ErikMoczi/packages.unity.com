namespace ut
{
  export class Resources
  {
    static assets = {}

    static register(name: string, entity: ut.Entity) {
      Resources.assets[name] = entity;
    }

    static exists(name: string): boolean {
      return name in Resources.assets;
    }

    static getByName(name: string) : ut.Entity {
      return Resources.assets[name];
    }
  }
}