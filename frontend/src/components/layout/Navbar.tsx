import { Link, useNavigate } from 'react-router-dom';
import { ShoppingCart, User, LogOut, Package, BarChart2 } from 'lucide-react';
import { useAuthStore } from '../../store/auth.store';
import { useCartStore } from '../../store/cart.store';
import { authApi } from '../../api/auth.api';

export function Navbar() {
  const { isAuthenticated, isAdmin, user, logout } = useAuthStore();
  const totalItems = useCartStore((s) => s.totalItems());
  const navigate = useNavigate();

  const handleLogout = async () => {
    await authApi.logout().catch(() => {});
    logout();
    navigate('/');
  };

  return (
    <nav className="bg-white border-b border-gray-200 px-6 py-3 flex items-center justify-between">
      <Link to="/" className="text-xl font-bold text-green-700">🏕️ OutdoorsShop</Link>

      <div className="flex items-center gap-4">
        <Link to="/catalog" className="text-sm text-gray-600 hover:text-green-700">Catalog</Link>

        {isAdmin && (
          <>
            <Link to="/admin/products" className="flex items-center gap-1 text-sm text-gray-600 hover:text-green-700">
              <Package size={16} /> Products
            </Link>
            <Link to="/admin/inventory" className="flex items-center gap-1 text-sm text-gray-600 hover:text-green-700">
              <BarChart2 size={16} /> Inventory
            </Link>
          </>
        )}

        <Link to="/cart" className="relative">
          <ShoppingCart size={22} className="text-gray-600 hover:text-green-700" />
          {totalItems > 0 && (
            <span className="absolute -top-2 -right-2 bg-green-600 text-white text-xs rounded-full w-5 h-5 flex items-center justify-center">
              {totalItems}
            </span>
          )}
        </Link>

        {isAuthenticated ? (
          <div className="flex items-center gap-3">
            <Link to="/profile" className="flex items-center gap-1 text-sm text-gray-600 hover:text-green-700">
              <User size={16} /> {user?.name}
            </Link>
            <button onClick={handleLogout} className="text-gray-500 hover:text-red-600">
              <LogOut size={18} />
            </button>
          </div>
        ) : (
          <Link to="/login" className="text-sm bg-green-700 text-white px-3 py-1.5 rounded-md hover:bg-green-800">
            Sign in
          </Link>
        )}
      </div>
    </nav>
  );
}
