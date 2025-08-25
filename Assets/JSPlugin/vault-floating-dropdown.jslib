mergeInto(LibraryManager.library, {
  DisableVaultButton: function () {
    var vaultDropdown = document.querySelector('floating-dropdown');
    console.log("Debug [JSPlugin > DisableVaultButton]");
    if (vaultDropdown) { 
      vaultDropdown.remove();
    } else {
      console.warn("[Vault Plugin] Failed attempt to remove element <floating-dropdown>")
    }
  },
});