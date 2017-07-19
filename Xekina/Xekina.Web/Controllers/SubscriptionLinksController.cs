using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Xekina.Web.Data;
using Xekina.Web.Models;

namespace Xekina.Web.Controllers
{
    public class SubscriptionLinksController : Controller
    {
        private readonly XekinaWebContext _context;

        public SubscriptionLinksController(XekinaWebContext context)
        {
            _context = context;    
        }

        // GET: SubscriptionLinks
        public async Task<IActionResult> Index()
        {
            return View(await _context.SubscriptionLink.ToListAsync());
        }

        // GET: SubscriptionLinks/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subscriptionLink = await _context.SubscriptionLink
                .SingleOrDefaultAsync(m => m.ID == id);
            if (subscriptionLink == null)
            {
                return NotFound();
            }

            return View(subscriptionLink);
        }

        // GET: SubscriptionLinks/Create
        public IActionResult Create()
        {
            return View();
        }

        // POST: SubscriptionLinks/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("ID,SubscriptionId,SubscriptionName,Validated")] SubscriptionLink subscriptionLink)
        {
            if (ModelState.IsValid)
            {
                _context.Add(subscriptionLink);
                await _context.SaveChangesAsync();
                return RedirectToAction("Index");
            }
            return View(subscriptionLink);
        }

        // GET: SubscriptionLinks/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subscriptionLink = await _context.SubscriptionLink.SingleOrDefaultAsync(m => m.ID == id);
            if (subscriptionLink == null)
            {
                return NotFound();
            }
            return View(subscriptionLink);
        }

        // POST: SubscriptionLinks/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("ID,SubscriptionId,SubscriptionName,Validated")] SubscriptionLink subscriptionLink)
        {
            if (id != subscriptionLink.ID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(subscriptionLink);
                    await _context.SaveChangesAsync();
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!SubscriptionLinkExists(subscriptionLink.ID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction("Index");
            }
            return View(subscriptionLink);
        }

        // GET: SubscriptionLinks/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var subscriptionLink = await _context.SubscriptionLink
                .SingleOrDefaultAsync(m => m.ID == id);
            if (subscriptionLink == null)
            {
                return NotFound();
            }

            return View(subscriptionLink);
        }

        // POST: SubscriptionLinks/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var subscriptionLink = await _context.SubscriptionLink.SingleOrDefaultAsync(m => m.ID == id);
            _context.SubscriptionLink.Remove(subscriptionLink);
            await _context.SaveChangesAsync();
            return RedirectToAction("Index");
        }

        private bool SubscriptionLinkExists(int id)
        {
            return _context.SubscriptionLink.Any(e => e.ID == id);
        }
    }
}
