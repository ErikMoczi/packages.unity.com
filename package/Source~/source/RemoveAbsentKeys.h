#include <unordered_map>
#include <unordered_set>
#include <vector>

// Removes entries from @map which are not present in @keys
template<
    typename T_KEY,
    typename T_VALUE,
    typename T_HASH>
void RemoveAbsentKeys(
    const std::unordered_set<T_KEY, T_HASH>& keys,
    std::unordered_map<T_KEY, T_VALUE, T_HASH>* map)
{
    std::vector<T_KEY> keysToRemove;
    for (const auto& iter : *map)
        if (keys.find(iter.first) == keys.end())
            keysToRemove.push_back(iter.first);

    for (const auto& key : keysToRemove)
        map->erase(key);
}
