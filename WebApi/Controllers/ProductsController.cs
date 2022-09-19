﻿using WebApi.Models;

namespace WebApi.Controllers;

[Route("api/[controller]")]
[ApiController]
public class ProductsController : ControllerBase
{
    private readonly DbContextClass _context;
    private readonly ICacheService _cacheService;
    private readonly int _expirationSeconds;
    private readonly IMapper _mapper;
    public ProductsController(DbContextClass context, ICacheService cacheService, IMapper mapper)
    {
        _context = context;
        _cacheService = cacheService;
        _expirationSeconds = int.Parse(ConfigurationManager.AppSetting["RedisCacheExpirationSeconds"]);
        _mapper = mapper;

    }

    // GET: api/Products
    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductDTO>>> GetProducts()
    {
        var productCache = _cacheService.GetData<List<ProductDTO>>("Products");
        int a = 0;
        int s = 9 / a;
        
        if (productCache == null)
        {
            var products = await _context.Products.Select(product => _mapper.Map<ProductDTO>(product)).ToListAsync();

            if (products != null)
            {
                productCache = products;
                _cacheService.SetData("Products", products, DateTimeOffset.Now.AddSeconds(_expirationSeconds));
            }

            return products;
        }

        return productCache;
    }


    // GET: api/Products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDTO>> GetProduct(int id)
    {
        var productCache = _cacheService.GetData<ProductDTO>("Product" + id);
        if (productCache == null)
        {
            //var product = await _context.Products.FindAsync(id);

            var product = await _context.Products
                .Where(p => p.Id == id)
                //.Take(10).Skip(20)
                //.Count()
                //.OrderBy(p => p.Id)
                //.MaxAsync(p => p.Id)
                //.Select(new Errorlog { k.Id, k.Category, k.Price });
                //.Select(p => new { p.Id, p.Name, p.Price })
                //.Union(_context.Products.Where(p => p.Id == id))
                .FirstOrDefaultAsync();
                
            if (product == null)
            {
                return NoContent();
            }

            
            _cacheService.SetData("Product" + id, product, DateTimeOffset.Now.AddSeconds(_expirationSeconds));

            return  _mapper.Map<ProductDTO>(product);
        }
        return productCache;

    }

    // PUT: api/Products/5
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<IActionResult> PutProduct(int id, ProductDTO productDto)
    {
        if (id != productDto.Id)
        {
            return BadRequest();
        } 

        _context.Entry(_mapper.Map<Product>(productDto)).State = EntityState.Modified;

        try
        {
            _cacheService.SetData("Product" + productDto.Id, productDto, DateTimeOffset.Now);
            await _context.SaveChangesAsync();
            
        }
        catch (DbUpdateConcurrencyException)
        {
            if (!ProductExists(id))
            {
                return NotFound();
            }
            else
            {
                throw;
            }
        }

        return NoContent();
    }

    // POST: api/Products
    // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<Product>> PostProduct(Product product)
    {
        if (_context.Products == null)
        {
            return Problem("Entity set 'DbContextClass.Products'  is null.");
        }
        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        _cacheService.SetData("Product" + product.Id, product, DateTimeOffset.Now.AddSeconds(_expirationSeconds));

        return CreatedAtAction("GetProduct", new { id = product.Id }, product);
    }

    // DELETE: api/Products/5
    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteProduct(int id)
    {
        if (_context.Products == null)
        {
            return NotFound();
        }
        var product = await _context.Products.FindAsync(id);
        if (product == null)
        {
            return NotFound();
        }

        _context.Products.Remove(product);
        _cacheService.RemoveData("Product");
        await _context.SaveChangesAsync();

        return NoContent();
    }

    private bool ProductExists(int id)
    {
        return (_context.Products?.Any(e => e.Id == id)).GetValueOrDefault();
    }
}
