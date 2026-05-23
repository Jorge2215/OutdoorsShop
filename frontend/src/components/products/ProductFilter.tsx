import type { Category } from '../../types/category';

interface ProductFilterProps {
  categories: Category[];
  selectedCategoryId?: number;
  searchTerm: string;
  onCategoryChange: (id?: number) => void;
  onSearchChange: (term: string) => void;
}

export function ProductFilter({
  categories,
  selectedCategoryId,
  searchTerm,
  onCategoryChange,
  onSearchChange,
}: ProductFilterProps) {
  return (
    <div className="flex flex-wrap gap-3 items-center">
      <input
        type="text"
        placeholder="Search products..."
        value={searchTerm}
        onChange={(e) => onSearchChange(e.target.value)}
        className="border border-gray-300 rounded-md px-3 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-green-600"
      />
      <div className="flex gap-2 flex-wrap">
        <button
          onClick={() => onCategoryChange(undefined)}
          className={`px-3 py-1.5 rounded-full text-sm font-medium transition-colors ${
            !selectedCategoryId
              ? 'bg-green-700 text-white'
              : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
          }`}
        >
          All
        </button>
        {categories.map((cat) => (
          <button
            key={cat.categoryID}
            onClick={() => onCategoryChange(cat.categoryID)}
            className={`px-3 py-1.5 rounded-full text-sm font-medium transition-colors ${
              selectedCategoryId === cat.categoryID
                ? 'bg-green-700 text-white'
                : 'bg-gray-100 text-gray-600 hover:bg-gray-200'
            }`}
          >
            {cat.name}
          </button>
        ))}
      </div>
    </div>
  );
}
