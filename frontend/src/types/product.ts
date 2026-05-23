export interface Product {
  productID: number;
  name: string;
  categoryID: number;
  categoryName: string;
  price: number;
  description?: string;
  imageUrl?: string;
  isActive: boolean;
  quantityAvailable: number;
}

export interface CreateProductRequest {
  name: string;
  categoryID: number;
  price: number;
  description?: string;
  imageUrl?: string;
}

export interface UpdateProductRequest extends Partial<CreateProductRequest> {
  isActive?: boolean;
}
